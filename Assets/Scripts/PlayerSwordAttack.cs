using UnityEngine;

/// <summary>
/// A temporary close-range player sword strike. The mobile control queues one
/// attack per tap; Space provides an Editor-testing fallback.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerSwordAttack : MonoBehaviour
{
    [SerializeField] private MobileAttackButton mobileAttackButton;
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField, Min(0.1f)] private float attackRange = 2f;
    [SerializeField, Min(0.05f)] private float attackRadius = 0.65f;
    [SerializeField, Min(0.05f)] private float attackCooldown = 0.45f;
    [SerializeField] private LayerMask hittableLayers = ~0;

    private readonly Collider[] hitBuffer = new Collider[16];
    private float nextAttackTime;

    private void Awake()
    {
        // Runtime bootstrap configuration may arrive after Awake but before Start.
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
        nextAttackTime = Time.time + attackCooldown;
        Vector3 strikeCenter = transform.position + transform.forward * attackRange;
        // NavMeshAgents and the CharacterController can both move in Update.
        // Sync once per attack so this physics query observes their latest poses.
        Physics.SyncTransforms();
        int hitCount = Physics.OverlapSphereNonAlloc(
            strikeCenter,
            attackRadius,
            hitBuffer,
            hittableLayers,
            QueryTriggerInteraction.Collide);

        EnemyHealth closestEnemy = null;
        float closestDistanceSquared = float.PositiveInfinity;

        for (int index = 0; index < hitCount; index++)
        {
            EnemyHealth enemy = hitBuffer[index].GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            float distanceSquared = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distanceSquared < closestDistanceSquared)
            {
                closestEnemy = enemy;
                closestDistanceSquared = distanceSquared;
            }
        }

        return closestEnemy != null && closestEnemy.TakeDamage(attackDamage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackRange, attackRadius);
    }
}
