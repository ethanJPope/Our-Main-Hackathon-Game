using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Deliberate mini-boss melee loop: acquire, approach, wind up, contact, recover.
/// NavMesh owns translation while the Animator and procedural aim own presentation.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public sealed class ShadowWardenAI : MonoBehaviour, IAnimationContactReceiver
{
    private enum State
    {
        Idle,
        Chase,
        Windup,
        Strike,
        Recovery,
        Dead
    }

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int LocomotionPlayback = Animator.StringToHash("LocomotionPlayback");
    private static readonly int Attack = Animator.StringToHash("Attack");

    [SerializeField] private Transform target;
    [SerializeField] private PlayerVitals targetVitals;
    [SerializeField] private Animator animator;
    [SerializeField] private HumanoidAnimationRuntime proceduralMotion;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float detectionRange = 12f;
    [SerializeField, Min(0.1f)] private float movementSpeed = 1.35f;
    [SerializeField, Min(0.1f)] private float attackRange = 2.05f;
    [SerializeField, Min(0f)] private float turnSpeed = 320f;
    [SerializeField, Min(0.02f)] private float pathRefreshInterval = 0.18f;

    [Header("Target visibility")]
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask lineOfSightMask = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0f)] private float eyeHeight = 1.35f;
    [SerializeField, Min(0f)] private float targetAimHeight = 0.9f;

    [Header("Heavy strike")]
    [SerializeField, Min(0f)] private float attackDamage = 20f;
    [SerializeField, Min(0.05f)] private float windupDuration = 0.55f;
    [SerializeField, Min(0.02f)] private float strikeDuration = 0.2f;
    [SerializeField, Min(0.05f)] private float recoveryDuration = 1.0f;
    [SerializeField, Min(0.05f)] private float attackCooldown = 1.75f;
    [SerializeField, Range(1f, 180f)] private float attackArcDegrees = 105f;

    private NavMeshAgent agent;
    private EnemyHealth health;
    private State state;
    private float stateEndsAt;
    private float nextPathRefreshAt;
    private float nextAttackAt;
    private bool contactDelivered;
    private Collider[] ownColliders;
    private readonly RaycastHit[] lineOfSightHits = new RaycastHit[8];
    private NavMeshPath targetPath;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        ownColliders = GetComponentsInChildren<Collider>();
        targetPath = new NavMeshPath();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (proceduralMotion == null && animator != null)
        {
            proceduralMotion = animator.GetComponent<HumanoidAnimationRuntime>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Died += OnDied;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Died -= OnDied;
        }
    }

    private void Start()
    {
        if (target == null || targetVitals == null || !IsTargetVitalsPairValid() || animator == null)
        {
            Debug.LogError($"{nameof(ShadowWardenAI)} on {name} is missing its player or Animator reference.", this);
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError($"{nameof(ShadowWardenAI)} on {name} must start on the baked NavMesh.", this);
            enabled = false;
            return;
        }

        agent.speed = movementSpeed;
        agent.angularSpeed = turnSpeed;
        agent.acceleration = 6f;
        agent.stoppingDistance = attackRange * 0.82f;
    }

    public void ConfigureTarget(Transform playerTransform, PlayerVitals playerVitals)
    {
        targetVitals = playerVitals;
        target = playerVitals != null ? playerVitals.transform : playerTransform;
    }

    private void Update()
    {
        if (state == State.Dead || target == null || targetVitals == null || targetVitals.CurrentHealth <= 0f)
        {
            StopMoving();
            SetMovementAnimation(0f);
            return;
        }

        float distance = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up).magnitude;
        if (distance > detectionRange && state == State.Idle)
        {
            StopMoving();
            SetMovementAnimation(0f);
            return;
        }

        switch (state)
        {
            case State.Idle:
            case State.Chase:
                TickChase(distance);
                break;
            case State.Windup:
                TickWindup();
                break;
            case State.Strike:
                TickStrike();
                break;
            case State.Recovery:
                TickRecovery();
                break;
        }
    }

    public void OnAnimationAttackContact()
    {
        if ((state != State.Windup && state != State.Strike) || contactDelivered || !CanHitTarget())
        {
            return;
        }

        contactDelivered = true;
        Vector3 direction = (target.position - transform.position).normalized;
        targetVitals.ApplyDamage(attackDamage, target.position + Vector3.up, direction);

        if (state == State.Windup)
        {
            state = State.Strike;
            stateEndsAt = Time.time + strikeDuration;
        }
    }

    private void TickChase(float distance)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            state = State.Idle;
            SetMovementAnimation(0f);
            proceduralMotion?.SetLocomotionVelocity(Vector3.zero, movementSpeed);
            return;
        }

        if (distance <= attackRange && Time.time >= nextAttackAt && CanHitTarget())
        {
            BeginAttack();
            return;
        }

        state = State.Chase;
        agent.isStopped = false;
        if (Time.time >= nextPathRefreshAt)
        {
            MeleeContactUtility.TrySetCompletePath(agent, target.position, targetPath);
            nextPathRefreshAt = Time.time + pathRefreshInterval;
        }

        float normalizedSpeed = Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f));
        SetMovementAnimation(normalizedSpeed);
        proceduralMotion?.SetLocomotionVelocity(agent.velocity, Mathf.Max(agent.speed, 0.01f));
    }

    private void BeginAttack()
    {
        StopMoving();
        FaceTarget();
        contactDelivered = false;
        nextAttackAt = Time.time + attackCooldown;
        state = State.Windup;
        stateEndsAt = Time.time + windupDuration;
        animator.SetTrigger(Attack);
        proceduralMotion?.BeginAttackAim(target.position + Vector3.up, windupDuration + strikeDuration + 0.35f);
    }

    private void TickWindup()
    {
        StopMoving();
        FaceTarget();
        if (!CanHitTarget())
        {
            state = State.Chase;
            return;
        }

        if (Time.time >= stateEndsAt)
        {
            state = State.Strike;
            stateEndsAt = Time.time + strikeDuration;
        }
    }

    private void TickStrike()
    {
        StopMoving();
        if (Time.time >= stateEndsAt)
        {
            if (!contactDelivered && CanHitTarget())
            {
                OnAnimationAttackContact();
            }

            state = State.Recovery;
            stateEndsAt = Time.time + recoveryDuration;
        }
    }

    private void TickRecovery()
    {
        StopMoving();
        FaceTarget();
        if (Time.time >= stateEndsAt)
        {
            state = State.Chase;
        }
    }

    private bool CanHitTarget()
    {
        if (target == null)
        {
            return false;
        }

        float distance = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up).magnitude;
        return distance <= attackRange + 0.25f
            && IsFacingTarget()
            && MeleeContactUtility.HasCompletePath(agent, target.position, targetPath)
            && (!requireLineOfSight || HasLineOfSight());
    }

    private bool IsFacingTarget()
    {
        return MeleeContactUtility.IsWithinArc(transform.position, transform.forward, target.position, attackArcDegrees);
    }

    private void FaceTarget()
    {
        Vector3 direction = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        proceduralMotion?.SetLocomotionVelocity(Vector3.zero, movementSpeed);
    }

    private bool HasLineOfSight()
    {
        return MeleeContactUtility.HasLineOfSight(
            transform,
            target,
            eyeHeight,
            targetAimHeight,
            lineOfSightMask,
            ownColliders,
            lineOfSightHits);
    }

    private bool IsTargetVitalsPairValid()
    {
        Transform vitalsTransform = targetVitals.transform;
        return target == vitalsTransform || target.IsChildOf(vitalsTransform) || vitalsTransform.IsChildOf(target);
    }

    private void SetMovementAnimation(float normalizedSpeed)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(Speed, normalizedSpeed, 0.12f, Time.deltaTime);
        animator.SetFloat(LocomotionPlayback, Mathf.Lerp(0.62f, 0.88f, normalizedSpeed));
    }

    private void OnDied(EnemyHealth _)
    {
        state = State.Dead;
        StopMoving();
        if (agent != null)
        {
            agent.enabled = false;
        }

        enabled = false;
    }
}
