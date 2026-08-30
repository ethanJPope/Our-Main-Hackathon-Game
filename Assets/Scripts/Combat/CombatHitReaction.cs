using UnityEngine;

/// <summary>
/// Adds a short directional recoil after the Animator has evaluated. Authored hit
/// and death clips remain primary; this layer keeps impacts directional even when
/// several character types share the same retargeted clips.
/// </summary>
[DefaultExecutionOrder(250)]
[DisallowMultipleComponent]
public sealed class CombatHitReaction : MonoBehaviour
{
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Die = Animator.StringToHash("Die");

    [SerializeField] private Animator animator;
    [SerializeField, Min(0.05f)] private float reactionDuration = 0.28f;
    [SerializeField, Range(0f, 25f)] private float maximumChestAngle = 12f;
    [SerializeField, Range(0f, 15f)] private float maximumHeadAngle = 6f;

    private Transform chest;
    private Transform head;
    private Vector3 reactionDirection;
    private float reactionStartedAt = float.NegativeInfinity;
    private EnemyHealth enemyHealth;
    private PlayerVitals playerVitals;

    public bool IsReacting => Time.time - reactionStartedAt < reactionDuration;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator != null && animator.isHuman)
        {
            chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (chest == null)
            {
                chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            }

            head = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        enemyHealth = GetComponent<EnemyHealth>();
        playerVitals = GetComponent<PlayerVitals>();
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.Damaged += OnEnemyDamaged;
            enemyHealth.Died += OnEnemyDied;
        }

        if (playerVitals != null)
        {
            playerVitals.Damaged += OnPlayerDamaged;
            playerVitals.Died += OnPlayerDied;
        }
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.Damaged -= OnEnemyDamaged;
            enemyHealth.Died -= OnEnemyDied;
        }

        if (playerVitals != null)
        {
            playerVitals.Damaged -= OnPlayerDamaged;
            playerVitals.Died -= OnPlayerDied;
        }
    }

    private void LateUpdate()
    {
        float normalizedTime = (Time.time - reactionStartedAt) / reactionDuration;
        if (normalizedTime < 0f || normalizedTime >= 1f || animator == null)
        {
            return;
        }

        float weight = Mathf.Sin(normalizedTime * Mathf.PI);
        Vector3 localDirection = transform.InverseTransformDirection(reactionDirection);
        Vector3 rotationAxis = new Vector3(-localDirection.z, 0f, localDirection.x).normalized;

        if (chest != null)
        {
            chest.localRotation *= Quaternion.AngleAxis(maximumChestAngle * weight, rotationAxis);
        }

        if (head != null)
        {
            head.localRotation *= Quaternion.AngleAxis(-maximumHeadAngle * weight, rotationAxis);
        }
    }

    public void PlayReaction(Vector3 hitDirection)
    {
        reactionDirection = Vector3.ProjectOnPlane(hitDirection, Vector3.up).normalized;
        if (reactionDirection.sqrMagnitude <= 0.001f)
        {
            reactionDirection = -transform.forward;
        }

        reactionStartedAt = Time.time;
        animator?.SetTrigger(Hit);
    }

    private void OnEnemyDamaged(EnemyHealth _, float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        PlayReaction(hitDirection);
    }

    private void OnEnemyDied(EnemyHealth _)
    {
        animator?.SetTrigger(Die);
    }

    private void OnPlayerDamaged(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        PlayReaction(hitDirection);
    }

    private void OnPlayerDied()
    {
        animator?.SetTrigger(Die);
    }
}
