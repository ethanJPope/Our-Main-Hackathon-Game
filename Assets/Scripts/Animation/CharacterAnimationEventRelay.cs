using UnityEngine;

/// <summary>
/// AnimationEvents execute on the GameObject that owns the Animator. Character
/// gameplay normally lives on a parent root, so this relay forwards the event
/// without coupling imported clips to one concrete combat component.
/// </summary>
[DisallowMultipleComponent]
public sealed class CharacterAnimationEventRelay : MonoBehaviour
{
    private MonoBehaviour[] parentBehaviours;

    private void Awake()
    {
        RefreshReceivers();
    }

    private void OnEnable()
    {
        RefreshReceivers();
    }

    private void RefreshReceivers()
    {
        parentBehaviours = GetComponentsInParent<MonoBehaviour>(true);
    }

    public void AnimationAttackContact()
    {
        RefreshReceivers();

        IAnimationContactReceiver activeReceiver = null;
        for (int index = 0; index < parentBehaviours.Length; index++)
        {
            MonoBehaviour behaviour = parentBehaviours[index];
            if (!behaviour.isActiveAndEnabled || behaviour is not IAnimationContactReceiver receiver)
            {
                continue;
            }

            if (activeReceiver != null)
            {
                Debug.LogError($"{nameof(CharacterAnimationEventRelay)} on {name} found multiple active contact receivers.", this);
                return;
            }

            activeReceiver = receiver;
        }

        activeReceiver?.OnAnimationAttackContact();
    }
}
