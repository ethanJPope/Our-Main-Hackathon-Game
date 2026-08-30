using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gives a presentation-only mini boss a small, deliberate patrol.  It has no
/// combat behavior yet: its purpose is to make the imported character visibly
/// walk while later boss mechanics are still being built.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class MiniBossSlowPatrol : MonoBehaviour
{
    private static readonly int Speed = Animator.StringToHash("Speed");

    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField, Min(0.1f)] private float movementSpeed = 0.8f;
    [SerializeField, Min(0.25f)] private float patrolDistance = 2f;
    [SerializeField, Min(0f)] private float pauseAtTurnaround = 0.6f;
    [SerializeField, Range(0.01f, 1f)] private float walkBlend = 0.45f;
    [SerializeField, Min(0.01f)] private float animationPlaybackSpeed = 0.65f;

    private Vector3 firstDestination;
    private Vector3 secondDestination;
    private bool headingToSecondDestination;
    private float resumeAt;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }

    private void Start()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogError($"{nameof(MiniBossSlowPatrol)} on {name} must be placed on a baked NavMesh.", this);
            enabled = false;
            return;
        }

        agent.speed = movementSpeed;
        agent.angularSpeed = 300f;
        agent.stoppingDistance = 0.05f;

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.speed = animationPlaybackSpeed;
            animator.SetFloat(Speed, walkBlend);
        }

        Vector3 start = transform.position;
        Vector3 forward = transform.forward * patrolDistance;
        firstDestination = SampleOnNavMesh(start + forward, start);
        secondDestination = SampleOnNavMesh(start - forward, start);
        headingToSecondDestination = false;
        agent.SetDestination(firstDestination);
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        if (Time.time < resumeAt)
        {
            return;
        }

        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.03f)
        {
            SetWalkVisual(true);
            return;
        }

        headingToSecondDestination = !headingToSecondDestination;
        agent.SetDestination(headingToSecondDestination ? secondDestination : firstDestination);
        resumeAt = Time.time + pauseAtTurnaround;
        SetWalkVisual(false);
    }

    private Vector3 SampleOnNavMesh(Vector3 preferredPoint, Vector3 fallbackPoint)
    {
        return NavMesh.SamplePosition(preferredPoint, out NavMeshHit hit, 1.5f, NavMesh.AllAreas)
            ? hit.position
            : fallbackPoint;
    }

    private void SetWalkVisual(bool isWalking)
    {
        if (animator != null)
        {
            animator.SetFloat(Speed, isWalking ? walkBlend : 0f);
        }
    }
}
