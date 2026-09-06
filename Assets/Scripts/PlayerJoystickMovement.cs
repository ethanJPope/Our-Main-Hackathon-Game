using UnityEngine;

/// <summary>
/// Camera-relative movement, lock-on strafing, and committed directional dodges.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerJoystickMovement : MonoBehaviour
{
    [SerializeField]
    private MobileJoystick joystick;

    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private MobileDodgeButton dodgeButton;

    [SerializeField]
    private PlayerVitals playerVitals;

    [SerializeField]
    private PlayerAnimationDriver animationDriver;

    [SerializeField, Min(0f)]
    private float movementSpeed = 5f;

    [Header("Movement Feel")]
    [SerializeField, Min(0.01f)]
    private float acceleration = 22f;

    [SerializeField, Min(0.01f)]
    private float turnAcceleration = 40f;

    [SerializeField, Min(0.01f)]
    private float deceleration = 30f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 12f;

    [SerializeField, Range(0f, 1f)]
    private float attackMovementMultiplier = 0.18f;

    [SerializeField]
    private float gravity = -20f;

    [SerializeField, Min(0f)]
    private float terminalFallSpeed = 40f;

    [Header("Dodge")]
    [SerializeField, Min(0f)]
    private float dodgeDistance = 4f;

    [SerializeField, Min(0.01f)]
    private float dodgeDuration = 0.18f;

    [SerializeField, Min(0f)]
    private float dodgeCooldown = 0.65f;

    [SerializeField, Min(0f)]
    private float dodgeStaminaCost = 15f;

    [SerializeField, Min(0f)]
    private float staminaRegenerationDelayAfterDodge = 1f;

    [SerializeField, Range(0f, 1f)]
    private float inputDeadZone = 0.05f;

    private CharacterController characterController;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float dodgeTimeRemaining;
    private float dodgeCooldownRemaining;
    private Vector3 dodgeDirection;
    private PlayerCombatInput combatInput;
    private PlayerCombatTargeting targeting;
    private PlayerSwordAttack attack;
    private CombatHitReaction hitReaction;
    [SerializeField, Range(0f, 1f)] private float invulnerabilityStart = 0.04f;
    [SerializeField, Range(0f, 1f)] private float invulnerabilityEnd = 0.22f;
    public bool IsDodging => isActiveAndEnabled && dodgeTimeRemaining > 0f;
    public bool IsDodgeRecovering => isActiveAndEnabled && dodgeCooldownRemaining > 0f;
    public bool IsInvulnerable => IsDodging && CanAct && dodgeDuration - dodgeTimeRemaining >= invulnerabilityStart && dodgeDuration - dodgeTimeRemaining < invulnerabilityEnd;
    public float DodgeCooldownRemaining => dodgeCooldownRemaining;
    public float DodgeStaminaCost => dodgeStaminaCost;

    public bool CanAct => isActiveAndEnabled && playerVitals != null && !playerVitals.IsDead && Time.timeScale > 0f && (hitReaction == null || !hitReaction.IsReacting);

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        combatInput = GetComponent<PlayerCombatInput>();
        targeting = GetComponent<PlayerCombatTargeting>();
        attack = GetComponent<PlayerSwordAttack>();
        hitReaction = GetComponent<CombatHitReaction>();
        if (playerVitals == null) playerVitals = GetComponent<PlayerVitals>();

        // A dynamic Rigidbody and CharacterController must not own the same
        // transform. Preserve old scenes safely if one is still attached.
        Rigidbody attachedBody = GetComponent<Rigidbody>();
        if (attachedBody != null)
        {
            attachedBody.isKinematic = true;
            attachedBody.useGravity = false;
        }

        if (joystick == null || cameraTransform == null || dodgeButton == null || playerVitals == null)
        {
            Debug.LogError(
                $"{nameof(PlayerJoystickMovement)} on {name} requires joystick, dodge button, camera, and vitals references.",
                this);
            enabled = false;
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;
        dodgeCooldownRemaining = Mathf.Max(0f, dodgeCooldownRemaining - deltaTime);

        if (!CanAct)
        {
            horizontalVelocity = Vector3.zero;
            dodgeTimeRemaining = 0f;
        }

        Vector2 input = CanAct ? (combatInput != null ? combatInput.Move : joystick.Direction) : Vector2.zero;

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 desiredDirection = cameraForward * input.y + cameraRight * input.x;
        float inputMagnitude = Mathf.Clamp01(input.magnitude);

        Vector3 facing = targeting != null && targeting.IsLocked ? targeting.FacingDirection : desiredDirection;
        if (facing.sqrMagnitude > inputDeadZone * inputDeadZone && dodgeTimeRemaining <= 0f && (attack == null || !attack.IsBusy))
        {
            Quaternion targetRotation = Quaternion.LookRotation(facing, Vector3.up);
            float rotationBlend = 1f - Mathf.Exp(-rotationSpeed * deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationBlend);
        }

        if (desiredDirection.sqrMagnitude > 0f) desiredDirection.Normalize();
        bool dodgeRequested = combatInput != null ? combatInput.HasBuffered(PlayerCombatInput.Action.Dodge) : dodgeButton.ConsumePress();
        if (dodgeRequested && TryDodge(desiredDirection))
        {
            combatInput?.Consume(PlayerCombatInput.Action.Dodge);
        }

        Vector3 horizontalDisplacement;
        if (dodgeTimeRemaining > 0f)
        {
            float dodgeStep = Mathf.Min(deltaTime, dodgeTimeRemaining);
            horizontalDisplacement = dodgeDirection * (dodgeDistance / dodgeDuration) * dodgeStep;
            dodgeTimeRemaining -= dodgeStep;
        }
        else
        {
            bool hasMovementInput = desiredDirection.sqrMagnitude > inputDeadZone * inputDeadZone;
            Vector3 targetVelocity = hasMovementInput
                ? desiredDirection * movementSpeed * inputMagnitude
                : Vector3.zero;

            if (attack != null && attack.IsBusy)
            {
                targetVelocity *= attackMovementMultiplier;
            }

            float velocityChangeRate = deceleration;
            if (hasMovementInput)
            {
                bool isReversingDirection = horizontalVelocity.sqrMagnitude > 0.01f &&
                    Vector3.Dot(horizontalVelocity.normalized, targetVelocity.normalized) < 0.5f;
                velocityChangeRate = isReversingDirection ? turnAcceleration : acceleration;
            }

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                velocityChangeRate * deltaTime);
            horizontalDisplacement = horizontalVelocity * deltaTime;
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity = Mathf.Max(verticalVelocity + gravity * deltaTime, -terminalFallSpeed);
        }

        Vector3 verticalDisplacement = Vector3.up * verticalVelocity * deltaTime;
        Vector3 positionBeforeMove = transform.position;
        CollisionFlags collisionFlags = characterController.Move(horizontalDisplacement + verticalDisplacement);
        if (dodgeTimeRemaining <= 0f && (collisionFlags & CollisionFlags.Sides) != 0)
        {
            Vector3 actualHorizontalVelocity = Vector3.ProjectOnPlane(
                transform.position - positionBeforeMove,
                Vector3.up) / Mathf.Max(deltaTime, Mathf.Epsilon);
            horizontalVelocity = actualHorizontalVelocity;
        }

        float animationSpeed = movementSpeed > 0f ? horizontalVelocity.magnitude / movementSpeed : 0f;
        animationDriver?.SetMovementSpeed(dodgeTimeRemaining > 0f ? 0f : animationSpeed);
        animationDriver?.SetLocomotionVelocity(
            dodgeTimeRemaining > 0f ? dodgeDirection * (dodgeDistance / dodgeDuration) : horizontalVelocity,
            dodgeTimeRemaining > 0f ? dodgeDistance / dodgeDuration : movementSpeed);
        animationDriver?.SetGrounded(characterController.isGrounded);
    }

    public bool TryDodge(Vector3 worldDirection)
    {
        if (!CanAct || !characterController.isGrounded || dodgeCooldownRemaining > 0f || IsDodging ||
            (attack != null && attack.IsBusy) || !playerVitals.TrySpend(PlayerResourceType.Stamina, dodgeStaminaCost)) return false;
        dodgeDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up).normalized;
        // Neutral dodge is a backstep; directional dodge uses the current input,
        // independent of the visual turn's interpolation.
        if (dodgeDirection.sqrMagnitude < 0.01f) dodgeDirection = -transform.forward;
        horizontalVelocity = Vector3.zero;
        playerVitals.DelayStaminaRegeneration(staminaRegenerationDelayAfterDodge);
        dodgeTimeRemaining = dodgeDuration;
        dodgeCooldownRemaining = dodgeCooldown;
        animationDriver?.PlayDodge();
        return true;
    }

    private void OnDisable()
    {
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        dodgeTimeRemaining = 0f;
        combatInput?.Clear();
    }
}
