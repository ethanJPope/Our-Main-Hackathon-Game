using UnityEngine;

/// <summary>
/// Moves the player camera-relatively from the temporary legacy mobile joystick.
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

    [SerializeField, Min(0f)]
    private float movementSpeed = 5f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 12f;

    [SerializeField]
    private float gravity = -20f;

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
    private float verticalVelocity;
    private float dodgeTimeRemaining;
    private float dodgeCooldownRemaining;
    private Vector3 dodgeDirection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

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
        dodgeCooldownRemaining = Mathf.Max(0f, dodgeCooldownRemaining - deltaTime);

        Vector2 input = joystick.Direction;

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 desiredDirection = cameraForward * input.y + cameraRight * input.x;
        float inputMagnitude = Mathf.Clamp01(input.magnitude);

        if (desiredDirection.sqrMagnitude > inputDeadZone * inputDeadZone && dodgeTimeRemaining <= 0f)
        {
            desiredDirection.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
            float rotationBlend = 1f - Mathf.Exp(-rotationSpeed * deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationBlend);
        }

        if (dodgeButton.ConsumePress() &&
            dodgeCooldownRemaining <= 0f &&
            dodgeTimeRemaining <= 0f &&
            playerVitals.TrySpend(PlayerResourceType.Stamina, dodgeStaminaCost))
        {
            dodgeDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            playerVitals.DelayStaminaRegeneration(staminaRegenerationDelayAfterDodge);
            dodgeTimeRemaining = dodgeDuration;
            dodgeCooldownRemaining = dodgeCooldown;
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
            horizontalDisplacement = desiredDirection.sqrMagnitude > inputDeadZone * inputDeadZone
                ? desiredDirection * movementSpeed * inputMagnitude * deltaTime
                : Vector3.zero;
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * deltaTime;
        }

        Vector3 verticalDisplacement = Vector3.up * verticalVelocity * deltaTime;
        characterController.Move(horizontalDisplacement + verticalDisplacement);
    }
}
