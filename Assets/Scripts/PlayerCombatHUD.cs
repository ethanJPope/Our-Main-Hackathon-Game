using UnityEngine;
using UnityEngine.UI;

/// <summary>Combat readiness, target marker, and safe-area layout.</summary>
public sealed class PlayerCombatHUD : MonoBehaviour
{
    [SerializeField] private PlayerVitals vitals;
    [SerializeField] private PlayerJoystickMovement movement;
    [SerializeField] private PlayerSwordAttack attack;
    [SerializeField] private PlayerCombatTargeting targeting;
    [SerializeField] private PlayerCombatInput input;
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private Image attackFill;
    [SerializeField] private Image dodgeFill;
    [SerializeField] private Text attackStatus;
    [SerializeField] private Text dodgeStatus;
    [SerializeField] private Text lockStatus;
    [SerializeField] private RectTransform reticle;
    private Rect lastSafeArea;
    private Vector2 lastScreenSize;
    private string lastAttackStatus;
    private string lastDodgeStatus;
    private void LateUpdate()
    {
        if (safeArea != null && (lastSafeArea != Screen.safeArea || lastScreenSize != new Vector2(Screen.width, Screen.height)))
        {
            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            // Unity Remote / Device Simulator may report the phone's safe area
            // while the standalone Game view uses a different pixel size.
            Rect usable = lastSafeArea;
            if (usable.width <= 0f || usable.height <= 0f || usable.xMax > Screen.width + 1 || usable.yMax > Screen.height + 1)
                usable = new Rect(0,0,Screen.width,Screen.height);
            safeArea.anchorMin = usable.min / lastScreenSize;
            safeArea.anchorMax = usable.max / lastScreenSize;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
        if (vitals == null || movement == null || attack == null) return;
        string a = vitals.IsDead ? "DEFEATED" : vitals.CurrentStamina < attack.AttackStaminaCost ? "LOW STAMINA" : attack.IsBusy || movement.IsDodgeRecovering ? "RECOVERING" : !attack.CanAttack ? "STAGGERED" : "ATTACK";
        string d = vitals.IsDead ? "DEFEATED" : vitals.CurrentStamina < movement.DodgeStaminaCost ? "LOW STAMINA" : movement.IsDodging ? "DODGING" : movement.DodgeCooldownRemaining > 0f || attack.IsBusy ? "RECOVERING" : "DODGE";
        if (a != lastAttackStatus) { attackStatus.text = a; lastAttackStatus = a; }
        if (d != lastDodgeStatus) { dodgeStatus.text = d; lastDodgeStatus = d; }
        attackFill.color = a == "ATTACK" ? new Color(0.85f, 0.64f, 0.34f, 0.85f) : new Color(0.3f, 0.3f, 0.3f, 0.55f);
        dodgeFill.color = movement.IsInvulnerable ? new Color(0.6f, 0.9f, 1f, 1f) : d == "DODGE" ? new Color(0.62f, 0.77f, 0.85f, 0.85f) : new Color(0.3f, 0.3f, 0.3f, 0.55f);
        bool locked = targeting != null && targeting.IsLocked;
        string label = locked ? "RELEASE" : "LOCK ON";
        if (lockStatus.text != label) lockStatus.text = label;
        if (reticle == null) return;
        bool visible = locked && input != null && input.ViewCamera != null;
        Vector3 screen = visible ? input.ViewCamera.WorldToScreenPoint(targeting.Target.transform.position + Vector3.up * 1.2f) : Vector3.zero;
        visible &= screen.z > 0f;
        if (reticle.gameObject.activeSelf != visible) reticle.gameObject.SetActive(visible);
        if (visible) reticle.position = new Vector3(screen.x, screen.y, 0f);
    }
}
