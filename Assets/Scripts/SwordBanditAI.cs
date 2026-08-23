using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A readable first melee enemy. It sees the player, approaches on a NavMesh,
/// telegraphs a single sword strike, applies damage only during the strike
/// window, then recovers before it can attack again.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class SwordBanditAI : MonoBehaviour
{
    public enum CombatState
    {
        Idle,
        Alert,
        Chase,
        Windup,
        Strike,
        Recovery,
        Dead
    }

    [SerializeField] private Transform target;
    [SerializeField] private PlayerVitals targetVitals;

    [Header("Awareness")]
    [SerializeField, Min(0f)] private float detectionRange = 9f;
    [SerializeField, Min(0f)] private float loseTargetRange = 12f;
    [SerializeField, Min(0f)] private float alertDuration = 0.15f;
    [SerializeField, Min(0f)] private float lostSightGraceDuration = 1.25f;
    [SerializeField, Min(0.02f)] private float perceptionInterval = 0.1f;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask lineOfSightMask = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0f)] private float eyeHeight = 1.1f;
    [SerializeField, Min(0f)] private float targetAimHeight = 0.9f;

    [Header("Melee attack")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.8f;
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField, Min(0.05f)] private float attackCooldown = 1.25f;
    [SerializeField, Min(0.05f)] private float windupDuration = 0.45f;
    [SerializeField, Min(0.02f)] private float strikeDuration = 0.12f;
    [SerializeField, Min(0.05f)] private float recoveryDuration = 0.65f;
    [SerializeField, Range(1f, 180f)] private float attackArcDegrees = 90f;
    [SerializeField, Min(0f)] private float turnSpeed = 540f;
    [SerializeField, Min(0.02f)] private float destinationRefreshInterval = 0.15f;
    [SerializeField, Tooltip("Enable after an attack clip calls AnimationStrikeHit at its contact frame.")]
    private bool useAnimationStrikeEvent;

    private EnemyHealth health;
    private NavMeshAgent agent;
    private CombatState currentState = CombatState.Idle;
    private float stateEndsAt;
    private float nextAttackStartTime;
    private float nextPerceptionTime;
    private float nextDestinationRefreshTime;
    private float lastTimeSawTarget = float.NegativeInfinity;
    private Vector3 lastKnownTargetPosition;
    private bool hasTarget;
    private bool strikeDelivered;
    private bool warnedAboutNavMesh;
    private Collider[] ownColliders;
    private readonly RaycastHit[] lineOfSightHits = new RaycastHit[8];

    public CombatState CurrentState => currentState;
    public bool HasTarget => hasTarget;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();
        ownColliders = GetComponentsInChildren<Collider>();
    }

    private void Start()
    {
        if (target == null || targetVitals == null)
        {
            Debug.LogError(
                $"{nameof(SwordBanditAI)} on {name} requires player Transform and {nameof(PlayerVitals)} references.",
                this);
            enabled = false;
        }
    }

    public void ConfigureTarget(Transform playerTransform, PlayerVitals playerVitals)
    {
        target = playerTransform;
        targetVitals = playerVitals;
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

    private void Update()
    {
        if (health.IsDead)
        {
            SetState(CombatState.Dead);
            StopMoving(false);
            return;
        }

        if (target == null || targetVitals == null || targetVitals.CurrentHealth <= 0f)
        {
            hasTarget = false;
            SetState(CombatState.Idle);
            StopMoving(false);
            return;
        }

        UpdateAwareness();

        if (!hasTarget)
        {
            SetState(CombatState.Idle);
            StopMoving(false);
            return;
        }

        switch (currentState)
        {
            case CombatState.Idle:
                BeginAlert();
                break;

            case CombatState.Alert:
                StopMoving(true);
                FaceTarget();
                if (Time.time >= stateEndsAt)
                {
                    SetState(CombatState.Chase);
                }
                break;

            case CombatState.Chase:
                TickChase();
                break;

            case CombatState.Windup:
                TickWindup();
                break;

            case CombatState.Strike:
                TickStrike();
                break;

            case CombatState.Recovery:
                TickRecovery();
                break;
        }
    }

    /// <summary>
    /// Called by a future attack animation at the exact sword-contact frame.
    /// The state machine remains the authority: late, duplicate, or cancelled
    /// animation events cannot cause a second hit.
    /// </summary>
    public void AnimationStrikeHit()
    {
        if (currentState != CombatState.Strike || strikeDelivered)
        {
            return;
        }

        DeliverStrike();
    }

    private void UpdateAwareness()
    {
        if (Time.time < nextPerceptionTime)
        {
            return;
        }

        nextPerceptionTime = Time.time + perceptionInterval;
        float distanceToTarget = PlanarDistance(transform.position, target.position);
        bool canSeeTarget = !requireLineOfSight || HasLineOfSight();

        if (canSeeTarget)
        {
            lastTimeSawTarget = Time.time;
            lastKnownTargetPosition = target.position;
        }

        if (!hasTarget)
        {
            hasTarget = canSeeTarget && distanceToTarget <= detectionRange;
            return;
        }

        bool outOfRange = distanceToTarget > loseTargetRange;
        bool sightExpired = Time.time > lastTimeSawTarget + lostSightGraceDuration;
        if (outOfRange || sightExpired)
        {
            hasTarget = false;
            SetState(CombatState.Idle);
        }
    }

    private void BeginAlert()
    {
        StopMoving(true);
        FaceTarget();
        SetState(CombatState.Alert, alertDuration);
    }

    private void TickChase()
    {
        if (!agent.isOnNavMesh)
        {
            if (!warnedAboutNavMesh)
            {
                Debug.LogError($"{nameof(SwordBanditAI)} on {name} is not positioned on a baked NavMesh.", this);
                warnedAboutNavMesh = true;
            }

            enabled = false;
            return;
        }

        float distanceToTarget = PlanarDistance(transform.position, target.position);
        if (CanStartAttack(distanceToTarget))
        {
            BeginWindup();
            return;
        }

        agent.isStopped = false;
        agent.updateRotation = true;
        agent.stoppingDistance = attackRange;
        if (Time.time >= nextDestinationRefreshTime)
        {
            agent.SetDestination(lastKnownTargetPosition);
            nextDestinationRefreshTime = Time.time + destinationRefreshInterval;
        }
    }

    private bool CanStartAttack(float distanceToTarget)
    {
        return Time.time >= nextAttackStartTime
            && distanceToTarget <= attackRange
            && IsFacingTarget()
            && (!requireLineOfSight || HasLineOfSight());
    }

    private void BeginWindup()
    {
        StopMoving(true);
        FaceTarget();
        strikeDelivered = false;
        nextAttackStartTime = Time.time + attackCooldown;
        SetState(CombatState.Windup, windupDuration);
    }

    private void TickWindup()
    {
        StopMoving(true);
        FaceTarget();

        if (!CanMaintainAttack())
        {
            SetState(CombatState.Chase);
            return;
        }

        if (Time.time >= stateEndsAt)
        {
            SetState(CombatState.Strike, strikeDuration);
            if (!useAnimationStrikeEvent)
            {
                DeliverStrike();
            }
        }
    }

    private void TickStrike()
    {
        StopMoving(true);
        if (!useAnimationStrikeEvent && !strikeDelivered)
        {
            DeliverStrike();
        }

        if (Time.time >= stateEndsAt)
        {
            SetState(CombatState.Recovery, recoveryDuration);
        }
    }

    private void TickRecovery()
    {
        StopMoving(true);
        FaceTarget();
        if (Time.time >= stateEndsAt)
        {
            SetState(CombatState.Chase);
        }
    }

    private bool CanMaintainAttack()
    {
        return hasTarget
            && PlanarDistance(transform.position, target.position) <= attackRange
            && IsFacingTarget()
            && (!requireLineOfSight || HasLineOfSight());
    }

    private void DeliverStrike()
    {
        if (strikeDelivered || !CanMaintainAttack())
        {
            return;
        }

        strikeDelivered = true;
        targetVitals.Modify(PlayerResourceType.Health, -attackDamage);
    }

    private void StopMoving(bool lockRotation)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.updateRotation = !lockRotation;
        }
    }

    private void FaceTarget()
    {
        Vector3 direction = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            desiredRotation,
            turnSpeed * Time.deltaTime);
    }

    private bool IsFacingTarget()
    {
        Vector3 direction = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return true;
        }

        float minimumDot = Mathf.Cos(attackArcDegrees * 0.5f * Mathf.Deg2Rad);
        return Vector3.Dot(transform.forward, direction.normalized) >= minimumDot;
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 aimPoint = target.position + Vector3.up * targetAimHeight;
        Vector3 toTarget = aimPoint - origin;
        float distance = toTarget.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return true;
        }

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            toTarget / distance,
            lineOfSightHits,
            distance,
            lineOfSightMask,
            QueryTriggerInteraction.Ignore);

        Collider closestRelevantHit = null;
        float closestDistance = float.PositiveInfinity;
        for (int index = 0; index < hitCount; index++)
        {
            Collider hitCollider = lineOfSightHits[index].collider;
            if (hitCollider == null || IsOwnCollider(hitCollider))
            {
                continue;
            }

            if (lineOfSightHits[index].distance < closestDistance)
            {
                closestDistance = lineOfSightHits[index].distance;
                closestRelevantHit = hitCollider;
            }
        }

        if (closestRelevantHit == null)
        {
            return true;
        }

        Transform hitTransform = closestRelevantHit.transform;
        return hitTransform == target || hitTransform.IsChildOf(target) || target.IsChildOf(hitTransform);
    }

    private bool IsOwnCollider(Collider collider)
    {
        for (int index = 0; index < ownColliders.Length; index++)
        {
            if (ownColliders[index] == collider)
            {
                return true;
            }
        }

        return false;
    }

    private void SetState(CombatState nextState, float duration = 0f)
    {
        if (currentState == nextState && duration <= 0f)
        {
            return;
        }

        currentState = nextState;
        stateEndsAt = Time.time + duration;
    }

    private void OnDied(EnemyHealth _)
    {
        SetState(CombatState.Dead);
        StopMoving(false);
        if (agent != null)
        {
            agent.enabled = false;
        }

        enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private static float PlanarDistance(Vector3 from, Vector3 to)
    {
        return Vector3.ProjectOnPlane(to - from, Vector3.up).magnitude;
    }
}
