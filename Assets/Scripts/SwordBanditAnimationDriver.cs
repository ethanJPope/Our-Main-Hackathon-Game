using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drives the bandit's presentation from the existing combat state machine.
/// Movement and damage remain owned by NavMeshAgent and SwordBanditAI.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SwordBanditAI))]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class SwordBanditAnimationDriver : MonoBehaviour
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int LocomotionPlayback = Animator.StringToHash("LocomotionPlayback");
    private static readonly int Attack = Animator.StringToHash("Attack");

    [SerializeField] private Animator animator;
    [SerializeField] private SwordBanditAI banditAi;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private HumanoidAnimationRuntime proceduralMotion;
    [SerializeField, Min(0f)] private float movementBlendTime = 0.1f;
    [SerializeField, Min(0.05f)] private float attackAnimationDuration = 1.05f;

    private SwordBanditAI.CombatState previousState;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (banditAi == null)
        {
            banditAi = GetComponent<SwordBanditAI>();
        }

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (animator == null || banditAi == null || agent == null)
        {
            Debug.LogWarning($"{nameof(SwordBanditAnimationDriver)} on {name} is missing a required reference.", this);
            enabled = false;
            return;
        }

        // The NavMeshAgent remains the authority for all translation.
        animator.applyRootMotion = false;
        if (proceduralMotion == null)
        {
            proceduralMotion = animator.GetComponent<HumanoidAnimationRuntime>();
        }

        previousState = banditAi.CurrentState;
    }

    private void OnDisable()
    {
        if (animator != null)
        {
            animator.SetFloat(Speed, 0f);
        }
    }

    private void Update()
    {
        if (animator == null || banditAi == null || agent == null)
        {
            return;
        }

        var state = banditAi.CurrentState;
        var isChasing = state == SwordBanditAI.CombatState.Chase;
        var normalizedSpeed = isChasing && agent.isOnNavMesh && !agent.isStopped
            ? Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f))
            : 0f;

        animator.SetFloat(Speed, normalizedSpeed, movementBlendTime, Time.deltaTime);
        animator.SetFloat(LocomotionPlayback, Mathf.Lerp(0.78f, 1.05f, normalizedSpeed));
        proceduralMotion?.SetLocomotionVelocity(agent.velocity, Mathf.Max(agent.speed, 0.01f));

        if (state != previousState && state == SwordBanditAI.CombatState.Windup)
        {
            animator.SetTrigger(Attack);
            Transform target = banditAi.Target;
            Vector3 aimPoint = target != null
                ? target.position + Vector3.up * 0.95f
                : transform.position + transform.forward * 1.5f + Vector3.up;
            proceduralMotion?.BeginAttackAim(aimPoint, attackAnimationDuration);
        }

        previousState = state;
    }
}
