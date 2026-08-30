using UnityEngine;

/// <summary>
/// Keeps player animation presentation synchronized with the existing movement,
/// dodge, and attack systems. Movement itself remains code-driven.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerAnimationDriver : MonoBehaviour
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int LocomotionPlayback = Animator.StringToHash("LocomotionPlayback");
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int Dodge = Animator.StringToHash("Dodge");
    private static readonly int Punch = Animator.StringToHash("Punch");

    [SerializeField] private Animator animator;
    [SerializeField] private HumanoidAnimationRuntime proceduralMotion;
    [SerializeField, Min(0f)] private float movementBlendTime = 0.08f;
    [SerializeField, Min(0.05f)] private float attackMovementLockDuration = 0.72f;

    private float attackMovementLockEndsAt;

    public bool IsAttackMovementLocked => Time.time < attackMovementLockEndsAt;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            Debug.LogWarning($"{nameof(PlayerAnimationDriver)} on {name} could not find an Animator.", this);
            enabled = false;
            return;
        }

        // PlayerJoystickMovement owns all translation, including the dodge.
        animator.applyRootMotion = false;

        if (proceduralMotion == null)
        {
            proceduralMotion = animator.GetComponent<HumanoidAnimationRuntime>();
        }
    }

    private void OnDisable()
    {
        if (animator != null)
        {
            animator.SetFloat(Speed, 0f);
        }
    }

    public void SetMovementSpeed(float normalizedSpeed)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(
            Speed,
            Mathf.Clamp01(normalizedSpeed),
            movementBlendTime,
            Time.deltaTime);
        animator.SetFloat(LocomotionPlayback, Mathf.Lerp(0.78f, 1.08f, Mathf.Clamp01(normalizedSpeed)));
    }

    public void SetGrounded(bool isGrounded)
    {
        if (animator != null)
        {
            animator.SetBool(Grounded, isGrounded);
        }
    }

    public void SetLocomotionVelocity(Vector3 worldVelocity, float referenceSpeed)
    {
        proceduralMotion?.SetLocomotionVelocity(worldVelocity, referenceSpeed);
    }

    public void PlayDodge()
    {
        if (animator != null)
        {
            animator.SetTrigger(Dodge);
        }
    }

    public void PlayPunch()
    {
        Vector3 fallbackTarget = transform.position + transform.forward * 1.25f + Vector3.up * 1.05f;
        PlayForwardPunch(fallbackTarget, attackMovementLockDuration);
    }

    public void PlayForwardPunch(Vector3 targetPoint, float duration)
    {
        if (animator != null)
        {
            animator.SetTrigger(Punch);
            float lockDuration = Mathf.Max(0.05f, duration);
            attackMovementLockEndsAt = Time.time + lockDuration;
            proceduralMotion?.BeginAttackAim(targetPoint, lockDuration);
        }
    }
}
