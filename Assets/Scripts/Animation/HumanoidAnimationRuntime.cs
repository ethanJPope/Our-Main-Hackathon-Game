using UnityEngine;

/// <summary>
/// Adds lightweight runtime polish after authored Humanoid clips: grounded feet,
/// a small pelvis correction, target-aware look direction, and temporary hand IK
/// that turns the existing punch into a readable forward strike.
/// </summary>
[DefaultExecutionOrder(150)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class HumanoidAnimationRuntime : MonoBehaviour
{
    private static readonly int Grounded = Animator.StringToHash("Grounded");

    [SerializeField] private Animator animator;
    [SerializeField] private Transform characterRoot;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0.05f)] private float footProbeHeight = 0.42f;
    [SerializeField, Min(0.05f)] private float footProbeDistance = 2.5f;
    [SerializeField, Min(0f)] private float soleOffset = 0.025f;
    [SerializeField, Min(0.1f)] private float footBlendSpeed = 12f;
    [SerializeField, Range(0f, 0.6f)] private float maximumPelvisDrop = 0.42f;
    [SerializeField, Range(0.02f, 0.25f)] private float footPlantHeight = 0.09f;
    [SerializeField, Range(0.03f, 0.35f)] private float footReleaseHeight = 0.15f;

    [Header("Combat aim")]
    [SerializeField, Range(0f, 1f)] private float lookWeight = 0.38f;
    [SerializeField, Range(0f, 1f)] private float headWeight = 0.72f;
    [SerializeField, Range(0f, 1f)] private float bodyWeight = 0.18f;
    [SerializeField, Range(0f, 1f)] private float maximumHandIkWeight = 0.88f;
    [SerializeField, Range(0.3f, 0.95f)] private float contactPhase = 0.46f;
    [SerializeField, Range(0.05f, 0.45f)] private float contactBlendWidth = 0.22f;

    [Header("Locomotion balance")]
    [SerializeField, Range(0f, 12f)] private float maximumForwardLeanDegrees = 4.5f;
    [SerializeField, Range(0f, 10f)] private float maximumLateralLeanDegrees = 2.5f;
    [SerializeField, Range(0f, 14f)] private float maximumTurnTwistDegrees = 5f;
    [SerializeField, Min(0.1f)] private float locomotionBalanceResponse = 10f;

    private readonly RaycastHit[] groundHits = new RaycastHit[8];
    private float leftFootWeight;
    private float rightFootWeight;
    private Vector3 leftFootPosition;
    private Vector3 rightFootPosition;
    private Quaternion leftFootRotation;
    private Quaternion rightFootRotation;
    private bool leftFootGrounded;
    private bool rightFootGrounded;

    private Vector3 attackAimPoint;
    private float attackAimStartedAt;
    private float attackAimDuration;
    private bool attackAimActive;
    private Vector2 desiredLocalLocomotion;
    private Vector2 smoothedLocalLocomotion;

    public Transform RightHand { get; private set; }
    public Vector3 CurrentContactPoint => RightHand != null
        ? RightHand.position
        : characterRoot.position + characterRoot.forward * 1.1f + Vector3.up;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        characterRoot = transform.parent != null ? transform.parent : transform;
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (characterRoot == null)
        {
            characterRoot = transform.parent != null ? transform.parent : transform;
        }

        if (animator == null || !animator.isHuman || animator.avatar == null || !animator.avatar.isValid)
        {
            Debug.LogWarning($"{nameof(HumanoidAnimationRuntime)} on {name} needs a valid Humanoid Animator.", this);
            enabled = false;
            return;
        }

        animator.applyRootMotion = false;
        // Older scene instances serialized the original short probe values.
        // Enforce a long probe, but keep the pelvis adjustment conservative;
        // the foot IK solver handles the remainder without burying the rig.
        footProbeDistance = Mathf.Max(footProbeDistance, 2.5f);
        maximumPelvisDrop = Mathf.Clamp(maximumPelvisDrop, 0.32f, 0.42f);
        RightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        bool grounded = EvaluateGrounded();
        animator.SetBool(Grounded, grounded);

        if (attackAimActive && Time.time >= attackAimStartedAt + attackAimDuration)
        {
            attackAimActive = false;
        }

        float blend = 1f - Mathf.Exp(-locomotionBalanceResponse * Time.deltaTime);
        smoothedLocalLocomotion = Vector2.Lerp(smoothedLocalLocomotion, desiredLocalLocomotion, blend);
    }

    public void BeginAttackAim(Vector3 worldPoint, float duration)
    {
        attackAimPoint = worldPoint;
        attackAimDuration = Mathf.Max(0.05f, duration);
        attackAimStartedAt = Time.time;
        attackAimActive = true;
    }

    /// <summary>
    /// Supplies the procedural layer with the actual code/NavMesh velocity.
    /// This keeps the upper-body balance tied to real movement rather than a
    /// clip's assumed root motion, which is disabled for this project.
    /// </summary>
    public void SetLocomotionVelocity(Vector3 worldVelocity, float referenceSpeed)
    {
        if (characterRoot == null)
        {
            return;
        }

        Vector3 localVelocity = characterRoot.InverseTransformDirection(
            Vector3.ProjectOnPlane(worldVelocity, Vector3.up));
        float safeReferenceSpeed = Mathf.Max(referenceSpeed, 0.01f);
        desiredLocalLocomotion = Vector2.ClampMagnitude(
            new Vector2(localVelocity.x, localVelocity.z) / safeReferenceSpeed,
            1f);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !animator.isHuman)
        {
            return;
        }

        ProbeFoot(
            AvatarIKGoal.LeftFoot,
            leftFootGrounded,
            out leftFootGrounded,
            out leftFootPosition,
            out leftFootRotation);
        ProbeFoot(
            AvatarIKGoal.RightFoot,
            rightFootGrounded,
            out rightFootGrounded,
            out rightFootPosition,
            out rightFootRotation);

        bool grounded = EvaluateGrounded();
        float targetLeftWeight = grounded && leftFootGrounded ? 1f : 0f;
        float targetRightWeight = grounded && rightFootGrounded ? 1f : 0f;
        leftFootWeight = Mathf.MoveTowards(leftFootWeight, targetLeftWeight, footBlendSpeed * Time.deltaTime);
        rightFootWeight = Mathf.MoveTowards(rightFootWeight, targetRightWeight, footBlendSpeed * Time.deltaTime);

        ApplyPelvisCorrection();
        ApplyFoot(AvatarIKGoal.LeftFoot, leftFootWeight, leftFootPosition, leftFootRotation);
        ApplyFoot(AvatarIKGoal.RightFoot, rightFootWeight, rightFootPosition, rightFootRotation);
        ApplyLocomotionBalance();
        ApplyCombatAim();
    }

    private bool EvaluateGrounded()
    {
        CharacterController controller = characterRoot.GetComponent<CharacterController>();
        if (controller != null)
        {
            return controller.isGrounded;
        }

        return Physics.Raycast(
            characterRoot.position + Vector3.up * 0.25f,
            Vector3.down,
            0.55f,
            groundLayers,
            QueryTriggerInteraction.Ignore);
    }

    private void ProbeFoot(
        AvatarIKGoal goal,
        bool wasPlanted,
        out bool grounded,
        out Vector3 position,
        out Quaternion rotation)
    {
        Vector3 animatedPosition = animator.GetIKPosition(goal);
        Vector3 origin = animatedPosition + Vector3.up * footProbeHeight;
        // A pelvis adjustment from the previous animation evaluation can put an
        // animated foot below the floor. Anchor the next probe above the root so
        // it continues to see the same floor instead of losing the hit and
        // oscillating between floating and penetration.
        origin.y = Mathf.Max(origin.y, characterRoot.position.y + 1.25f);
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            groundHits,
            footProbeHeight + footProbeDistance + 1.25f,
            groundLayers,
            QueryTriggerInteraction.Ignore);

        grounded = false;
        position = animatedPosition;
        rotation = animator.GetIKRotation(goal);
        float closestDistance = float.PositiveInfinity;

        for (int index = 0; index < hitCount; index++)
        {
            Collider collider = groundHits[index].collider;
            if (collider == null || collider.transform == characterRoot || collider.transform.IsChildOf(characterRoot))
            {
                continue;
            }

            if (collider.GetComponentInParent<PlayerVitals>() != null ||
                collider.GetComponentInParent<EnemyHealth>() != null)
            {
                continue;
            }

            if (groundHits[index].distance >= closestDistance)
            {
                continue;
            }

            closestDistance = groundHits[index].distance;
            // A raised foot has a perfectly valid floor ray below it, but that
            // does not mean it is in stance. Locking it would erase the swing
            // phase and create a visible slide. The larger release distance
            // keeps a planted foot stable across tiny terrain/clip fluctuations.
            float plantThreshold = wasPlanted ? footReleaseHeight : footPlantHeight;
            grounded = animatedPosition.y - groundHits[index].point.y <= plantThreshold;
            if (!grounded)
            {
                continue;
            }

            position = groundHits[index].point + groundHits[index].normal * soleOffset;
            Vector3 footForward = Vector3.ProjectOnPlane(characterRoot.forward, groundHits[index].normal);
            if (footForward.sqrMagnitude > 0.001f)
            {
                rotation = Quaternion.LookRotation(footForward.normalized, groundHits[index].normal);
            }
        }
    }

    private void ApplyFoot(AvatarIKGoal goal, float weight, Vector3 position, Quaternion rotation)
    {
        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKRotationWeight(goal, weight);
        animator.SetIKPosition(goal, position);
        animator.SetIKRotation(goal, rotation);
    }

    private void ApplyPelvisCorrection()
    {
        if (!leftFootGrounded && !rightFootGrounded)
        {
            return;
        }

        float leftDelta = leftFootGrounded
            ? leftFootPosition.y - animator.GetIKPosition(AvatarIKGoal.LeftFoot).y
            : 0f;
        float rightDelta = rightFootGrounded
            ? rightFootPosition.y - animator.GetIKPosition(AvatarIKGoal.RightFoot).y
            : 0f;
        float pelvisDelta = Mathf.Clamp(Mathf.Min(leftDelta, rightDelta), -maximumPelvisDrop, 0f);
        float pelvisWeight = Mathf.Min(leftFootGrounded ? leftFootWeight : 1f, rightFootGrounded ? rightFootWeight : 1f);
        animator.bodyPosition += Vector3.up * (pelvisDelta * pelvisWeight);
    }

    private void ApplyCombatAim()
    {
        if (!attackAimActive)
        {
            animator.SetLookAtWeight(0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            return;
        }

        float phase = Mathf.Clamp01((Time.time - attackAimStartedAt) / attackAimDuration);
        float phaseDistance = Mathf.Abs(phase - contactPhase) / Mathf.Max(contactBlendWidth, 0.01f);
        float contactWeight = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phaseDistance));

        animator.SetLookAtWeight(lookWeight, bodyWeight, headWeight, 0.35f, 0.6f);
        animator.SetLookAtPosition(attackAimPoint);

        Transform shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Transform forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        if (RightHand == null || shoulder == null || forearm == null)
        {
            return;
        }

        float maximumReach = (Vector3.Distance(shoulder.position, forearm.position)
            + Vector3.Distance(forearm.position, RightHand.position)) * 0.96f;
        Vector3 shoulderToAim = attackAimPoint - shoulder.position;
        Vector3 reachablePoint = shoulder.position + Vector3.ClampMagnitude(shoulderToAim, maximumReach);
        float handWeight = contactWeight * maximumHandIkWeight;

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, handWeight);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, handWeight * 0.7f);
        animator.SetIKPosition(AvatarIKGoal.RightHand, reachablePoint);
        animator.SetIKRotation(
            AvatarIKGoal.RightHand,
            Quaternion.LookRotation(characterRoot.forward, Vector3.up) * Quaternion.Euler(0f, 90f, 0f));
    }

    private void ApplyLocomotionBalance()
    {
        // The authored clips remain the primary performance. This is a subtle
        // procedural correction, reduced during attacks so it never pulls the
        // forward-punch contact out of its authored window.
        float attackAttenuation = attackAimActive ? 0.35f : 1f;
        float forwardLean = smoothedLocalLocomotion.y * maximumForwardLeanDegrees * attackAttenuation;
        float lateralLean = -smoothedLocalLocomotion.x * maximumLateralLeanDegrees * attackAttenuation;
        float turnTwist = smoothedLocalLocomotion.x * maximumTurnTwistDegrees * attackAttenuation;
        if (Mathf.Abs(forwardLean) < 0.01f && Mathf.Abs(lateralLean) < 0.01f && Mathf.Abs(turnTwist) < 0.01f)
        {
            return;
        }

        Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        HumanBodyBones balanceBone = chest != null ? HumanBodyBones.Chest : HumanBodyBones.Spine;
        Transform balanceTransform = chest != null
            ? chest
            : animator.GetBoneTransform(HumanBodyBones.Spine);
        if (balanceTransform == null)
        {
            return;
        }

        Quaternion correction = Quaternion.Euler(forwardLean, turnTwist, lateralLean);
        animator.SetBoneLocalRotation(balanceBone, balanceTransform.localRotation * correction);
    }
}
