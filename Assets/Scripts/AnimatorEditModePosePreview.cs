using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Keeps the stopped-scene preview on the same grounded Idle pose used at runtime.
/// This component is intentionally inactive during Play Mode; gameplay animation
/// drivers remain the runtime authority.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class AnimatorEditModePosePreview : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Range(0f, 1f)] private float normalizedTime;

    private bool sampleQueued;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        EnsureAnimatorReference();
        QueueSample();
    }

    private void OnValidate()
    {
        EnsureAnimatorReference();
        QueueSample();
    }

    private void OnDisable()
    {
        sampleQueued = false;
    }

    private void Update()
    {
        if (Application.isPlaying || !sampleQueued)
        {
            return;
        }

        sampleQueued = false;
        SampleIdlePose();
    }

    private void EnsureAnimatorReference()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void QueueSample()
    {
        if (Application.isPlaying)
        {
            return;
        }

        sampleQueued = true;
#if UNITY_EDITOR
        EditorApplication.QueuePlayerLoopUpdate();
#endif
    }

    private void SampleIdlePose()
    {
        if (Application.isPlaying || animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            return;
        }

        animator.Play("Idle", 0, normalizedTime);
        animator.Update(0f);
    }
}
