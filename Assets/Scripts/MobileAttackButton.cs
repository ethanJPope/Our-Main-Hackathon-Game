using UnityEngine;

/// <summary>
/// Converts a legacy touch or mouse press inside this UI area into one sword
/// attack request. It mirrors the dodge-control input behavior.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class MobileAttackButton : MonoBehaviour
{
    private RectTransform touchArea;
    private Canvas canvas;
    private Camera uiCamera;
    private int activeFingerId = -1;
    private bool mouseActive;
    private bool pressQueued;

    private void Awake()
    {
        touchArea = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        MobileHudTheme.ApplyControlPalette(canvas != null ? canvas.transform : transform);
        uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }

    private void Update()
    {
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
        pressQueued = false;
    }

    public bool ConsumePress()
    {
        if (!pressQueued)
        {
            return false;
        }

        pressQueued = false;
        return true;
    }

    private void ReadTouches()
    {
        bool activeTouchFound = false;

        for (int index = 0; index < Input.touchCount; index++)
        {
            Touch touch = Input.GetTouch(index);

            if (activeFingerId < 0 &&
                touch.phase == TouchPhase.Began &&
                RectTransformUtility.RectangleContainsScreenPoint(touchArea, touch.position, uiCamera))
            {
                activeFingerId = touch.fingerId;
                pressQueued = true;
            }

            if (touch.fingerId != activeFingerId)
            {
                continue;
            }

            activeTouchFound = true;

            if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled)
            {
                activeFingerId = -1;
            }
        }

        if (activeFingerId >= 0 && !activeTouchFound)
        {
            activeFingerId = -1;
        }
    }

    private void ReadMouse()
    {
        Vector2 mousePosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            mouseActive = RectTransformUtility.RectangleContainsScreenPoint(touchArea, mousePosition, uiCamera);
            if (mouseActive)
            {
                pressQueued = true;
            }
        }

        if (mouseActive && Input.GetMouseButtonUp(0))
        {
            mouseActive = false;
        }
    }
}
