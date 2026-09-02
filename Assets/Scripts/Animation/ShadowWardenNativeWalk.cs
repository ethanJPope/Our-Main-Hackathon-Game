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

    private Transform visualRoot;
    private Transform leftArm;
    private Transform rightArm;
    private Quaternion leftArmRestRotation;
    private Quaternion rightArmRestRotation;
    private Vector3 visualRootRestPosition;
    private bool initialized;

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

        InitializeRig();
    }

    private void OnDisable()
    {
        RestoreRestPose();
    }

    private void LateUpdate()
    {
        if (!initialized)
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

        // This rig's arm axes are stable around their local forward direction.
        // The conservative amplitude gives it deliberate movement without
        // pulling vertices beyond the generated weight envelope.
        leftArm.localRotation = leftArmRestRotation * Quaternion.AngleAxis(swing, Vector3.forward);
        rightArm.localRotation = rightArmRestRotation * Quaternion.AngleAxis(-swing, Vector3.forward);
        visualRoot.localPosition = visualRootRestPosition + Vector3.up * bob;
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
        if (visualRoot == null || leftArm == null || rightArm == null)
        {
            Debug.LogWarning($"{nameof(ShadowWardenNativeWalk)} could not resolve the Warden arm bones.", this);
            enabled = false;
            return;
        }

        visualRootRestPosition = visualRoot.localPosition;
        leftArmRestRotation = leftArm.localRotation;
        rightArmRestRotation = rightArm.localRotation;
        initialized = true;
    }

    private void RestoreRestPose()
    {
        if (!initialized)
        {
            return;
        }

        leftArm.localRotation = leftArmRestRotation;
        rightArm.localRotation = rightArmRestRotation;
        visualRoot.localPosition = visualRootRestPosition;
    }
}
