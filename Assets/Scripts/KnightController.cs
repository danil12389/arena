using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class KnightController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 12f;
    [Min(0f)] public float jumpHeight = 1.5f;
    public Animator animator;
    public Transform movementCamera;

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");

    private CharacterController controller;
    private float verticalSpeed;
    private float groundGraceRemaining;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.minMoveDistance = 0f; // Даже малые перемещения обновляют контакт с землёй.

        // Если поле не заполнено, используем камеру с тегом MainCamera.
        if (movementCamera == null && Camera.main != null)
            movementCamera = Camera.main.transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // Проверяем и текущее состояние, и переход в удар.
        bool isAttacking = false;
        if (animator != null)
        {
            // Ищем слой по имени: порядок слоёв Animator может измениться.
            int attackLayer = animator.GetLayerIndex("Upper Body");
            if (attackLayer >= 0)
            {
                isAttacking = animator.GetCurrentAnimatorStateInfo(attackLayer).IsTag("Attack");
                if (animator.IsInTransition(attackLayer))
                    isAttacking |= animator.GetNextAnimatorStateInfo(attackLayer).IsTag("Attack");
            }

            // GetMouseButtonDown срабатывает один раз при нажатии ЛКМ.
            if (Input.GetMouseButtonDown(0) && !isAttacking)
            {
                animator.SetTrigger(AttackParameter);
            }
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(x, 0f, z).normalized;
        if (movementCamera != null)
        {
            // Берём только поворот камеры вокруг вертикали: взгляд вверх не поднимает героя.
            Quaternion cameraYaw = Quaternion.Euler(0f, movementCamera.eulerAngles.y, 0f);
            direction = cameraYaw * direction;
        }

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // На неровностях isGrounded может кратковременно пропадать.
        bool isGrounded = controller.isGrounded;
        if (isGrounded && verticalSpeed <= 0f)
        {
            groundGraceRemaining = 0.15f;
            // Прижимаем капсулу к склону во время ходьбы.
            verticalSpeed = -Mathf.Max(2f, moveSpeed);
        }
        else
        {
            groundGraceRemaining = Mathf.Max(0f, groundGraceRemaining - Time.deltaTime);
        }

        // Даём 0.15 секунды запаса после потери контакта с землёй.
        if (Input.GetKeyDown(KeyCode.Space) && groundGraceRemaining > 0f
            && verticalSpeed <= 0f && Physics.gravity.y < 0f && jumpHeight > 0f)
        {
            // Из формулы v² = 2gh получаем скорость для заданной высоты.
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            groundGraceRemaining = 0f; // Расходуем прыжок: повторный в воздухе запрещён.
        }

        // Гравитация уменьшает скорость вверх, а затем тянет персонажа вниз.
        verticalSpeed += Physics.gravity.y * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalSpeed;
        Vector3 positionBeforeMove = transform.position;
        CollisionFlags collisions = controller.Move(velocity * Time.deltaTime);

        // При ударе головой о потолок прекращаем подъём сразу.
        if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
            verticalSpeed = 0f;

        // Для ходьбы нужна только горизонтальная скорость, без падения.
        Vector3 displacement = transform.position - positionBeforeMove;
        displacement.y = 0f;
        float speed = Time.deltaTime > 0f ? displacement.magnitude / Time.deltaTime : 0f;

        if (animator != null)
            animator.SetFloat(SpeedParameter, speed);
    }
}
