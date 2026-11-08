using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);
    [Min(0.5f)] public float distance = 4f;
    [Min(0f)] public float mouseSensitivity = 3f;
    public float minPitch = -25f;
    public float maxPitch = 70f;
    [Min(0.01f)] public float collisionRadius = 0.25f;
    [Min(0f)] public float returnSpeed = 8f;
    public LayerMask collisionLayers = ~0;

    private float yaw;
    private float pitch = 20f;
    private float currentDistance;

    private void Start()
    {
        // Начинаем позади персонажа, затем камера вращается независимо от него.
        yaw = target != null ? target.eulerAngles.y : transform.eulerAngles.y;
        currentDistance = distance;
        SetCursorLocked(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorLocked(false);
        else if (Input.GetMouseButtonDown(1))
            SetCursorLocked(true); // ПКМ возвращает управление камерой после Esc.

        if (target == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        // Mouse X/Y уже задают смещение мыши: умножать его на deltaTime не нужно.
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // LateUpdate выполняется после движения персонажа в Update.
        Vector3 pivot = target.position + targetOffset;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 backwards = rotation * Vector3.back;
        float allowedDistance = distance;

        // Проверяем объём камеры, а не один луч. Коллайдеры самого героя пропускаем.
        RaycastHit[] hits = Physics.SphereCastAll(
            pivot, collisionRadius, backwards, distance,
            collisionLayers, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            Transform obstacle = hit.collider.transform;
            if (obstacle == target || obstacle.IsChildOf(target))
                continue;

            allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, hit.distance - 0.05f));
        }

        // Перед стеной приближаемся сразу, обратно отъезжаем плавно.
        currentDistance = allowedDistance < currentDistance
            ? allowedDistance
            : Mathf.Lerp(currentDistance, allowedDistance, 1f - Mathf.Exp(-returnSpeed * Time.deltaTime));

        transform.SetPositionAndRotation(pivot + backwards * currentDistance, rotation);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}