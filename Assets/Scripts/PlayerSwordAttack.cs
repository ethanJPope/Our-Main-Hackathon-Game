using UnityEngine;

/// <summary>
/// Forward close-range player strike. Input starts the animation; damage is
/// resolved only at the authored contact frame (with a guarded timed fallback),
/// so the fist and gameplay hit agree.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerSwordAttack : MonoBehaviour, IAnimationContactReceiver
{
    [SerializeField] private MobileAttackButton mobileAttackButton;
    [SerializeField] private PlayerVitals playerVitals;
    [SerializeField] private PlayerAnimationDriver animationDriver;
    [SerializeField] private HumanoidAnimationRuntime proceduralMotion;
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.75f;
    [SerializeField, Min(0.05f)] private float attackRadius = 0.42f;
    [SerializeField, Min(0.05f)] private float attackCooldown = 0.72f;
    [SerializeField, Min(0.05f)] private float animationDuration = 0.72f;
    [SerializeField, Min(0.02f)] private float contactFallbackDelay = 0.36f;
    [SerializeField, Range(1f, 180f)] private float aimAssistArc = 75f;
    [SerializeField, Range(0f, 180f)] private float maximumAimSnapDegrees = 28f;
    [SerializeField] private LayerMask hittableLayers = ~0;

    private readonly Collider[] targetBuffer = new Collider[24];
    private float nextAttackTime;
    private float fallbackContactAt;
    private bool contactPending;
    private EnemyHealth assistedTarget;

    public bool CanAttack => playerVitals != null && !playerVitals.IsDead;

    private void Awake()
    {
        if (playerVitals == null)
        {
            playerVitals = GetComponent<PlayerVitals>();
        }

        if (animationDriver == null)
        {
            animationDriver = GetComponent<PlayerAnimationDriver>();
        }

        if (proceduralMotion == null)
        {
            proceduralMotion = GetComponentInChildren<HumanoidAnimationRuntime>(true);
        }
    }

    private void Start()
    {
        if (mobileAttackButton == null)
        {
            Debug.LogError($"{nameof(PlayerSwordAttack)} on {name} requires a {nameof(MobileAttackButton)} reference.", this);
        }
    }

    public void Configure(MobileAttackButton attackButton)
    {
        mobileAttackButton = attackButton;
    }

    private void Update()
    {
        if (!CanAttack)
        {
            ClearPendingContact();
            return;
        }

        if (contactPending && Time.time >= fallbackContactAt)
        {
            ResolveContact();
        }

        if (mobileAttackButton == null)
        {
            return;
        }

        if ((mobileAttackButton.ConsumePress() || Input.GetKeyDown(KeyCode.Space)) && Time.time >= nextAttackTime)
        {
            TryAttack();
        }
    }

    public bool TryAttack()
    {
        if (!CanAttack || Time.time < nextAttackTime || contactPending)
        {
            return false;
        }

        assistedTarget = FindBestTarget();
        Vector3 aimPoint = assistedTarget != null
            ? GetTargetAimPoint(assistedTarget)
            : transform.position + transform.forward * attackRange + Vector3.up * 1.05f;

        ApplyLimitedAimSnap(aimPoint);
        nextAttackTime = Time.time + attackCooldown;
        fallbackContactAt = Time.time + contactFallbackDelay;
        contactPending = true;
        animationDriver?.PlayForwardPunch(aimPoint, animationDuration);
        return true;
    }

    public void OnAnimationAttackContact()
    {
        ResolveContact();
    }

    private void OnDisable()
    {
        ClearPendingContact();
    }

    private void ClearPendingContact()
    {
        contactPending = false;
        assistedTarget = null;
    }

    private void ResolveContact()
    {
        if (!contactPending)
        {
            return;
        }

        contactPending = false;
        Physics.SyncTransforms();

        Vector3 handContactPoint = proceduralMotion != null
            ? proceduralMotion.CurrentContactPoint
            : transform.position + transform.forward * attackRange + Vector3.up;
        // Humanoid retargeting can leave the authored fist a little short even
        // when the target was validly acquired in front of the player. Blend
        // the gameplay contact toward that target so the assisted forward punch
        // and the damage volume agree without extending through distant targets.
        Vector3 contactPoint = assistedTarget != null && IsValidContact(assistedTarget)
            ? Vector3.Lerp(handContactPoint, GetTargetAimPoint(assistedTarget), 0.62f)
            : handContactPoint;
        Vector3 capsuleStart = contactPoint - transform.forward * 0.16f;
        Vector3 capsuleEnd = contactPoint + transform.forward * 0.24f;
        int hitCount = Physics.OverlapCapsuleNonAlloc(
            capsuleStart,
            capsuleEnd,
            attackRadius,
            targetBuffer,
            hittableLayers,
            QueryTriggerInteraction.Collide);

        EnemyHealth closestEnemy = null;
        float closestDistanceSquared = float.PositiveInfinity;
        for (int index = 0; index < hitCount; index++)
        {
            EnemyHealth enemy = targetBuffer[index].GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !IsValidContact(enemy))
            {
                continue;
            }

            float distanceSquared = (enemy.transform.position - contactPoint).sqrMagnitude;
            if (distanceSquared < closestDistanceSquared)
            {
                closestEnemy = enemy;
                closestDistanceSquared = distanceSquared;
            }
        }

        if (closestEnemy != null)
        {
            Vector3 direction = (closestEnemy.transform.position - transform.position).normalized;
            closestEnemy.TakeDamage(attackDamage, contactPoint, direction);
        }

        assistedTarget = null;
    }

    private EnemyHealth FindBestTarget()
    {
        Vector3 searchCenter = transform.position + transform.forward * (attackRange * 0.55f);
        int hitCount = Physics.OverlapSphereNonAlloc(
            searchCenter,
            attackRange,
            targetBuffer,
            hittableLayers,
            QueryTriggerInteraction.Collide);

        EnemyHealth bestTarget = null;
        float bestScore = float.PositiveInfinity;
        for (int index = 0; index < hitCount; index++)
        {
            EnemyHealth enemy = targetBuffer[index].GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !IsValidContact(enemy))
            {
                continue;
            }

            Vector3 toEnemy = enemy.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(toEnemy, Vector3.up));
            float score = toEnemy.sqrMagnitude + angle * 0.02f;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    private bool IsValidContact(EnemyHealth enemy)
    {
        float planarDistance = Vector3.ProjectOnPlane(enemy.transform.position - transform.position, Vector3.up).magnitude;
        return planarDistance <= attackRange + attackRadius
            && MeleeContactUtility.IsWithinArc(transform.position, transform.forward, enemy.transform.position, aimAssistArc);
    }

    private void ApplyLimitedAimSnap(Vector3 aimPoint)
    {
        Vector3 direction = Vector3.ProjectOnPlane(aimPoint - transform.position, Vector3.up);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, maximumAimSnapDegrees);
    }

    private static Vector3 GetTargetAimPoint(EnemyHealth target)
    {
        Collider targetCollider = target.GetComponentInChildren<Collider>();
        return targetCollider != null
            ? targetCollider.bounds.center
            : target.transform.position + Vector3.up;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackRange, attackRadius);
    }
}
