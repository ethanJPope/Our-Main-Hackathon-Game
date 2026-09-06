using UnityEngine;

/// <summary>One press per touch, including Unity Remote's legacy touch stream.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class CombatTouchButton : MonoBehaviour
{
    private RectTransform touchArea;
    private Camera uiCamera;
    private int activeFingerId = -1;
    private int sampledFrame = -1;
    private bool mouseActive;
    private bool pressQueued;
    private float pressedAt;
    public bool IsPressed { get { Sample(); return activeFingerId >= 0 || mouseActive; } }
    protected virtual void Awake()
    {
        touchArea = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }
    protected virtual void Update() => Sample();
    protected virtual void OnDisable() => ResetInput();
    private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
    private void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
    public bool ConsumePress()
    {
        Sample();
        bool pressed = pressQueued && Time.unscaledTime - pressedAt <= 0.2f;
        pressQueued = false;
        return pressed;
    }
    public bool Contains(Vector2 position) => isActiveAndEnabled && touchArea != null &&
        RectTransformUtility.RectangleContainsScreenPoint(touchArea, position, uiCamera);
    private void Sample()
    {
        if (!isActiveAndEnabled || sampledFrame == Time.frameCount) return;
        sampledFrame = Time.frameCount;
        if (Time.timeScale <= 0f) { ResetInput(); return; }
        bool found = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (activeFingerId < 0 && touch.phase == TouchPhase.Began && Contains(touch.position))
            {
                activeFingerId = touch.fingerId;
                QueuePress();
            }
            if (touch.fingerId != activeFingerId) continue;
            found = true;
            if (touch.phase == TouchPhase.Canceled) { activeFingerId = -1; pressQueued = false; }
            else if (touch.phase == TouchPhase.Ended) activeFingerId = -1;
        }
        if (!found) activeFingerId = -1;
        if (Input.touchCount > 0) { mouseActive = false; return; }
        if (Input.GetMouseButtonDown(0) && Contains(Input.mousePosition))
        {
            mouseActive = true;
            QueuePress();
        }
        if (!Input.GetMouseButton(0)) mouseActive = false;
    }
    private void QueuePress() { pressQueued = true; pressedAt = Time.unscaledTime; }
    private void ResetInput() { activeFingerId = -1; mouseActive = false; pressQueued = false; }
}
