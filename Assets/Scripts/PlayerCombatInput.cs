using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Shared touch/desktop commands with one expiring action buffer.</summary>
[DisallowMultipleComponent]
public sealed class PlayerCombatInput : MonoBehaviour
{
    public enum Action { None, Attack, Dodge }
    [SerializeField] private MobileJoystick joystick;
    [SerializeField] private MobileAttackButton attackButton;
    [SerializeField] private MobileDodgeButton dodgeButton;
    [SerializeField] private CombatTouchButton lockButton;
    [SerializeField] private Camera viewCamera;
    [SerializeField, Range(0.05f, 0.3f)] private float bufferDuration = 0.18f;
    [SerializeField] private KeyCode dodgeKey = KeyCode.Space;
    [SerializeField] private KeyCode lockKey = KeyCode.Tab;
    private PlayerVitals vitals;
    private PlayerCombatTargeting targeting;
    private int sampledFrame = -1;
    private Action pending;
    private float expiresAt;
    private Vector2 move;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private PointerEventData pointer;
    private EventSystem pointerSystem;
    public Vector2 Move { get { Sample(); return move; } }
    public Camera ViewCamera => viewCamera;
    public bool GameplayEnabled => isActiveAndEnabled && Time.timeScale > 0f && vitals != null && !vitals.IsDead;
    private void Awake()
    {
        vitals = GetComponent<PlayerVitals>();
        targeting = GetComponent<PlayerCombatTargeting>();
        if (viewCamera == null) viewCamera = Camera.main;
    }
    private void Update() => Sample();
    private void OnDisable() => Clear();
    private void OnApplicationFocus(bool focused) { if (!focused) Clear(); }
    private void OnApplicationPause(bool paused) { if (paused) Clear(); }
    public void Clear() { pending = Action.None; move = Vector2.zero; targeting?.ClearPointerAim(); }
    public void Queue(Action action)
    {
        if (!GameplayEnabled) return;
        pending = action;
        if (action != Action.Attack) targeting?.ClearPointerAim();
        expiresAt = Time.unscaledTime + bufferDuration;
    }
    public bool HasBuffered(Action action)
    {
        Sample();
        return GameplayEnabled && pending == action && Time.unscaledTime <= expiresAt;
    }
    public void Consume(Action action) { if (pending == action) pending = Action.None; }
    public bool IsOverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        if (pointer == null || pointerSystem != EventSystem.current)
        {
            pointerSystem = EventSystem.current;
            pointer = new PointerEventData(pointerSystem);
        }
        pointer.position = position;
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);
        return uiHits.Count > 0;
    }
    private void Sample()
    {
        if (sampledFrame == Time.frameCount) return;
        sampledFrame = Time.frameCount;
        bool touchAttack = attackButton != null && attackButton.ConsumePress();
        bool touchDodge = dodgeButton != null && dodgeButton.ConsumePress();
        bool touchLock = lockButton != null && lockButton.ConsumePress();
        if (!GameplayEnabled) { Clear(); return; }
        Vector2 keyboard = new Vector2(
            (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
            (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
        move = keyboard.sqrMagnitude > 0f ? Vector2.ClampMagnitude(keyboard, 1f) : joystick != null ? joystick.Direction : Vector2.zero;
        if (touchLock || Input.GetKeyDown(lockKey)) targeting?.ToggleLock();
        bool mouseAttack = Input.touchCount == 0 && Input.GetMouseButtonDown(0) && !IsOverUI(Input.mousePosition);
        if (touchAttack || mouseAttack)
        {
            if (mouseAttack && viewCamera != null) targeting?.AimAtPointer(viewCamera.ScreenPointToRay(Input.mousePosition));
            Queue(Action.Attack);
        }
        if (touchDodge || Input.GetKeyDown(dodgeKey)) Queue(Action.Dodge);
        if (Time.unscaledTime > expiresAt) { pending = Action.None; targeting?.ClearPointerAim(); }
    }
}
