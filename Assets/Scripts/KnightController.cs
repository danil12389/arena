using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class KnightController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 12f;
    public Animator animator;

    private CharacterController controller;
    private float verticalSpeed;
    private Vector3 lastPosition;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        lastPosition = transform.position;
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(x, 0f, z).normalized;

        // Поворот персонажа в сторону движения
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Гравитация
        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        verticalSpeed += Physics.gravity.y * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalSpeed;

        controller.Move(velocity * Time.deltaTime);

        // === Считаем скорость ПОСЛЕ движения ===
        float speed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        // Передаём реальную скорость в Animator
        if (animator != null)
        Debug.Log("Speed передаётся в Animator: " + speed);
       animator.SetFloat("Speed", speed);
    }
}
