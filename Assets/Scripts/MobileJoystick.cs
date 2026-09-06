using UnityEngine;

/// <summary>
/// Reads legacy mouse or touch input inside a fixed joystick area.
/// This temporarily keeps Unity Remote phone testing independent of the Input System.
/// </summary>
[DisallowMultipleComponent]
public sealed class MobileJoystick : MonoBehaviour
{
    [SerializeField]
    private RectTransform inputArea;

    [SerializeField]
    private RectTransform handle;

    [SerializeField, Min(1f)]
    private float movementRange = 64f;

    [SerializeField, Range(0f, 0.4f)] private float deadZone = 0.12f;
    private Vector2 direction;
    private int sampledFrame = -1;
    public Vector2 Direction { get { Sample(); return direction; } }

    private Canvas canvas;
    private Camera uiCamera;
    private Vector2 handleStartPosition;
    private int activeFingerId = -1;
    private bool mouseActive;

    private void Awake()
    {
        if (inputArea == null || handle == null)
        {
            Debug.LogError($"{nameof(MobileJoystick)} on {name} requires input area and handle references.", this);
            enabled = false;
            return;
        }

        canvas = GetComponentInParent<Canvas>();
        MobileHudTheme.ApplyControlPalette(canvas != null ? canvas.transform : transform);
        uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        handleStartPosition = handle.anchoredPosition;
    }

    private void Update()
    {
        Sample();
    }

    private void Sample()
    {
        if (!isActiveAndEnabled || sampledFrame == Time.frameCount) return;
        sampledFrame = Time.frameCount;
        if (Time.timeScale <= 0f) { OnDisable(); return; }
        ReadTouches();

        if (Input.touchCount == 0)
        {
            ReadMouse();
        }
    }

    private void OnDisable()
    {
        activeFingerId = -1;
        mouseActive = false;
        ResetHandle();
    }

    private void OnApplicationFocus(bool focused) { if (!focused) OnDisable(); }
    private void OnApplicationPause(bool paused) { if (paused) OnDisable(); }

    private void ReadTouches()
    {
        bool activeTouchFound = false;

        for (int index = 0; index < Input.touchCount; index++)
        {
            Touch touch = Input.GetTouch(index);

            if (activeFingerId < 0 &&
                touch.phase == TouchPhase.Began &&
                RectTransformUtility.RectangleContainsScreenPoint(inputArea, touch.position, uiCamera))
            {
                activeFingerId = touch.fingerId;
            }

            if (touch.fingerId != activeFingerId)
            {
                continue;
            }

            activeTouchFound = true;

            if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled)
            {
                activeFingerId = -1;
                ResetHandle();
            }
            else
            {
                UpdateHandle(touch.position);
            }
        }

        if (activeFingerId >= 0 && !activeTouchFound)
        {
            activeFingerId = -1;
            ResetHandle();
        }
    }

    private void ReadMouse()
    {
        Vector2 mousePosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            mouseActive = RectTransformUtility.RectangleContainsScreenPoint(inputArea, mousePosition, uiCamera);
        }

        if (mouseActive && Input.GetMouseButton(0))
        {
            UpdateHandle(mousePosition);
        }

        if (mouseActive && Input.GetMouseButtonUp(0))
        {
            mouseActive = false;
            ResetHandle();
        }
    }

    private void UpdateHandle(Vector2 screenPosition)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handle.parent as RectTransform,
                screenPosition,
                uiCamera,
                out Vector2 localPosition))
        {
            return;
        }

        Vector2 offset = Vector2.ClampMagnitude(localPosition - handleStartPosition, movementRange);
        handle.anchoredPosition = handleStartPosition + offset;
        float magnitude = offset.magnitude / movementRange;
        direction = magnitude <= deadZone ? Vector2.zero : offset.normalized * Mathf.InverseLerp(deadZone, 1f, magnitude);
    }

    private void ResetHandle()
    {
        direction = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = handleStartPosition;
        }
    }
}
