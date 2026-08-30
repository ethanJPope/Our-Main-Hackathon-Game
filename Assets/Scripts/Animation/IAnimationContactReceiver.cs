/// <summary>
/// Receives authored contact moments from an AnimationEvent relay. Gameplay
/// remains authoritative: receivers still validate range, facing, state, and
/// whether the contact was already consumed.
/// </summary>
public interface IAnimationContactReceiver
{
    void OnAnimationAttackContact();
}
