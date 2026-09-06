using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A small, source-rig-safe locomotion layer for the generated Shadow Warden.
/// It uses only the imported rig's own rest transforms and never retargets a
/// foreign Humanoid clip, preventing the destructive arm and root offsets that
/// the generated asset exhibits with the shared animation library.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class ShadowWardenNativeWalk : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField, Min(0.1f)] private float gaitCyclesPerSecond = 0.75f;
    [SerializeField, Range(0f, 20f)] private float armSwingDegrees = 8f;
    [SerializeField, Range(0f, 0.08f)] private float bodyBobDistance = 0.04f;
    [SerializeField, Range(0f, 75f)] private float attackArmDegrees = 54f;
    [SerializeField, Range(0f, 20f)] private float attackChestDegrees = 10f;
    [SerializeField, Min(0.1f)] private float deathFallDuration = 0.7f;
    [SerializeField, Range(5f, 45f)] private float deathFallDegrees = 18f;
    [SerializeField, Range(0f, 0.2f)] private float deathDropDistance = 0.08f;

    private Transform visualRoot;
    private Transform leftArm;
    private Transform rightArm;
    private Transform chest;
    private Transform spine;
    private Transform hips;
    private Transform leftUpperLeg;
    private Transform rightUpperLeg;
    private Transform leftLowerLeg;
    private Transform rightLowerLeg;
    private Transform leftFoot;
    private Transform rightFoot;
    private Quaternion leftArmRestRotation;
    private Quaternion rightArmRestRotation;
    private Quaternion chestRestRotation;
    private Quaternion spineRestRotation;
    private Quaternion hipsRestRotation;
    private Quaternion leftUpperLegRestRotation;
    private Quaternion rightUpperLegRestRotation;
    private Quaternion leftLowerLegRestRotation;
    private Quaternion rightLowerLegRestRotation;
    private Quaternion leftFootRestRotation;
    private Quaternion rightFootRestRotation;
    private Vector3 visualRootRestPosition;
    private Quaternion visualRootRestRotation;
    private bool initialized;
    private EnemyHealth health;
    private bool subscribed;
    private float attackStartedAt = float.NegativeInfinity;
    private float attackDuration = 1f;
    private float deathStartedAt = float.PositiveInfinity;

    public int AttackRequestCount { get; private set; }
    public bool IsAttacking => Time.time - attackStartedAt < attackDuration;
    public bool IsDying => Time.time >= deathStartedAt;

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

        health = GetComponent<EnemyHealth>();

        InitializeRig();
    }

    private void OnEnable()
    {
        if (health == null)
        {
            health = GetComponent<EnemyHealth>();
        }

        if (health != null && !subscribed)
        {
            health.Died += OnDied;
            subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (health != null && subscribed)
        {
            health.Died -= OnDied;
            subscribed = false;
        }

        RestoreRestPose();
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            return;
        }

        if (IsDying)
        {
            float deathTime = Mathf.Clamp01((Time.time - deathStartedAt) / Mathf.Max(deathFallDuration, 0.01f));
            float fall = Mathf.SmoothStep(0f, 1f, deathTime);
            leftArm.localRotation = leftArmRestRotation;
            rightArm.localRotation = rightArmRestRotation;
            chest.localRotation = NeutralizePosture(chestRestRotation);
            hips.localRotation = NeutralizePosture(hipsRestRotation);
            spine.localRotation = NeutralizePosture(spineRestRotation);
            hips.localRotation = hipsRestRotation;
            leftUpperLeg.localRotation = leftUpperLegRestRotation;
            rightUpperLeg.localRotation = rightUpperLegRestRotation;
            leftLowerLeg.localRotation = leftLowerLegRestRotation;
            rightLowerLeg.localRotation = rightLowerLegRestRotation;
            leftFoot.localRotation = leftFootRestRotation;
            rightFoot.localRotation = rightFootRestRotation;
            visualRoot.localPosition = visualRootRestPosition + Vector3.down * (deathDropDistance * fall);
            visualRoot.localRotation = visualRootRestRotation * Quaternion.AngleAxis(deathFallDegrees * fall, Vector3.forward);
            return;
        }

        // Native Humanoid clips own the full body when available. Keep this
        // legacy presentation fallback dormant so it cannot fight the
        // Animator by overwriting the same arm/chest/root transforms.
        if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
        {
            return;
        }

        float locomotion = agent != null && agent.enabled && agent.isOnNavMesh
            ? Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f))
            : 0f;

        // Keep a light breathing/sway loop while the boss is paused in melee,
        // then blend to the full deliberate walk as the NavMeshAgent moves.
        float presentation = Mathf.Lerp(0.35f, 1f, locomotion);
        float phase = Time.time * gaitCyclesPerSecond * Mathf.PI * 2f;
        float swing = Mathf.Sin(phase) * armSwingDegrees * presentation;
        float bob = Mathf.Abs(Mathf.Sin(phase)) * bodyBobDistance * presentation;
        float legSwing = Mathf.Sin(phase) * 7f * presentation;
        float kneeBend = Mathf.Max(0f, Mathf.Sin(phase)) * 4f * presentation;

        float attackTime = Mathf.Clamp01((Time.time - attackStartedAt) / Mathf.Max(attackDuration, 0.01f));
        float windup = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.34f, attackTime));
        float recovery = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 1f, attackTime));
        float attackWeight = IsAttacking ? windup * recovery : 0f;
        float strike = IsAttacking ? Mathf.Exp(-Mathf.Pow((attackTime - 0.52f) / 0.09f, 2f)) : 0f;

        // This rig's arm axes are stable around their local forward direction.
        // The conservative amplitude gives it deliberate movement without
        // pulling vertices beyond the generated weight envelope.
        leftArm.localRotation = leftArmRestRotation * Quaternion.AngleAxis(swing, Vector3.forward);
        rightArm.localRotation = rightArmRestRotation
            * Quaternion.AngleAxis(-swing - attackArmDegrees * attackWeight + attackArmDegrees * 0.42f * strike, Vector3.forward);
        chest.localRotation = NeutralizePosture(chestRestRotation)
            * Quaternion.AngleAxis(-attackChestDegrees * attackWeight + attackChestDegrees * 0.55f * strike, Vector3.up);
        spine.localRotation = NeutralizePosture(spineRestRotation);
        hips.localRotation = NeutralizePosture(hipsRestRotation) * Quaternion.AngleAxis(-legSwing * 0.25f, Vector3.forward);
        leftUpperLeg.localRotation = leftUpperLegRestRotation * Quaternion.AngleAxis(legSwing, Vector3.right);
        rightUpperLeg.localRotation = rightUpperLegRestRotation * Quaternion.AngleAxis(-legSwing, Vector3.right);
        leftLowerLeg.localRotation = leftLowerLegRestRotation * Quaternion.AngleAxis(kneeBend, Vector3.right);
        rightLowerLeg.localRotation = rightLowerLegRestRotation * Quaternion.AngleAxis(Mathf.Max(0f, -Mathf.Sin(phase)) * 4f * presentation, Vector3.right);
        leftFoot.localRotation = leftFootRestRotation * Quaternion.AngleAxis(-kneeBend * 0.5f, Vector3.right);
        rightFoot.localRotation = rightFootRestRotation * Quaternion.AngleAxis(-Mathf.Max(0f, -Mathf.Sin(phase)) * 2f * presentation, Vector3.right);
        visualRoot.localPosition = visualRootRestPosition + Vector3.up * bob;
    }

    public void BeginHeavyStrike(float duration)
    {
        if (!initialized || IsDying)
        {
            return;
        }

        attackStartedAt = Time.time;
        attackDuration = Mathf.Max(0.2f, duration);
        AttackRequestCount++;
    }

    private void InitializeRig()
    {
        if (animator == null)
        {
            enabled = false;
            return;
        }

        visualRoot = animator.transform;
        leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        chest = animator.GetBoneTransform(HumanBodyBones.UpperChest)
            ?? animator.GetBoneTransform(HumanBodyBones.Chest);
        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (visualRoot == null || leftArm == null || rightArm == null || chest == null
            || spine == null || hips == null || leftUpperLeg == null || rightUpperLeg == null
            || leftLowerLeg == null || rightLowerLeg == null || leftFoot == null || rightFoot == null)
        {
            Debug.LogWarning($"{nameof(ShadowWardenNativeWalk)} could not resolve the Warden arm bones.", this);
            enabled = false;
            return;
        }

        visualRootRestPosition = visualRoot.localPosition;
        visualRootRestRotation = visualRoot.localRotation;
        leftArmRestRotation = leftArm.localRotation;
        rightArmRestRotation = rightArm.localRotation;
        chestRestRotation = chest.localRotation;
        spineRestRotation = spine.localRotation;
        hipsRestRotation = hips.localRotation;
        leftUpperLegRestRotation = leftUpperLeg.localRotation;
        rightUpperLegRestRotation = rightUpperLeg.localRotation;
        leftLowerLegRestRotation = leftLowerLeg.localRotation;
        rightLowerLegRestRotation = rightLowerLeg.localRotation;
        leftFootRestRotation = leftFoot.localRotation;
        rightFootRestRotation = rightFoot.localRotation;
        initialized = true;
    }

    private static Quaternion NeutralizePosture(Quaternion rest)
    {
        Vector3 euler = rest.eulerAngles;
        return Quaternion.Euler(0f, euler.y, 0f);
    }

    private void RestoreRestPose()
    {
        if (!initialized)
        {
            return;
        }

        leftArm.localRotation = leftArmRestRotation;
        rightArm.localRotation = rightArmRestRotation;
        chest.localRotation = NeutralizePosture(chestRestRotation);
        spine.localRotation = NeutralizePosture(spineRestRotation);
        hips.localRotation = NeutralizePosture(hipsRestRotation);
        visualRoot.localPosition = visualRootRestPosition;
        visualRoot.localRotation = visualRootRestRotation;
    }

    private void OnDied(EnemyHealth _)
    {
        deathStartedAt = Time.time;
        attackStartedAt = float.NegativeInfinity;
    }
}
