using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChase : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [SerializeField, Min(0f)] private float stoppingDistance = 1.8f;
    [SerializeField] private Animator animator;

    private NavMeshAgent agent;
    private float nextPathUpdate;
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false; // Перемещением управляет агент, а не клип.
    }

    private void Update()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            UpdateAnimation(0f);
            return;
        }

        if (target == null)
        {
            agent.isStopped = true;
            UpdateAnimation(0f);
            return;
        }

        // Обновляем путь пять раз в секунду, а не каждый кадр.
        if (Time.time >= nextPathUpdate)
        {
            nextPathUpdate = Time.time + 0.2f;
            // Ищем точку на земле рядом с целью, даже когда рыцарь прыгает.
            if (NavMesh.SamplePosition(target.position, out NavMeshHit hit, 4f, agent.areaMask))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        // Анимация зависит от реального движения: у стены сундук не идёт на месте.
        UpdateAnimation(agent.velocity.magnitude);
    }

    private void UpdateAnimation(float speed)
    {
        if (animator != null)
            animator.SetFloat(SpeedParameter, speed, 0.1f, Time.deltaTime);
    }

    private void OnDisable()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.ResetPath();
    }
}