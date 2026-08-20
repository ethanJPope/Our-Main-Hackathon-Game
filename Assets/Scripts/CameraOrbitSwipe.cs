using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Rotates a Cinemachine orbital camera from legacy touch or mouse swipes.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineOrbitalFollow))]
public sealed class CameraOrbitSwipe : MonoBehaviour
{
    [SerializeField, Min(0.01f)]
    private float swipeSensitivity = 0.25f;

    [SerializeField, Range(0f, 1f)]
    private float minimumSwipeStartX = 0.35f;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    private CinemachineOrbitalFollow orbitalFollow;
    private PointerEventData pointerEventData;
    private int activeFingerId = -1;
    private bool mouseDragging;

    private void Awake()
    {
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
    }

    private void Update()
    {
        ReadTouchSwipe();

        if (Input.touchCount == 0)
        {
            ReadMouseDrag();
        }
    }

    private void OnDisable()
    {
        activeFingerId = -1;
        mouseDragging = false;
    }

    private void ReadTouchSwipe()
    {
        bool activeTouchFound = false;

        for (int index = 0; index < Input.touchCount; index++)
        {
            Touch touch = Input.GetTouch(index);

            if (activeFingerId < 0 && touch.phase == TouchPhase.Began)
            {
                bool startsInCameraArea = touch.position.x >= Screen.width * minimumSwipeStartX;
                if (startsInCameraArea && !IsPositionOverUi(touch.position))
                {
                    activeFingerId = touch.fingerId;
                }
            }

            if (touch.fingerId != activeFingerId)
            {
                continue;
            }

            activeTouchFound = true;

            if (touch.phase == TouchPhase.Moved)
            {
                ApplyHorizontalDelta(touch.deltaPosition.x);
            }

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

    private void ReadMouseDrag()
    {
        Vector2 mousePosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            bool startsInCameraArea = mousePosition.x >= Screen.width * minimumSwipeStartX;
            mouseDragging = startsInCameraArea && !IsPositionOverUi(mousePosition);
        }

        if (mouseDragging && Input.GetMouseButton(0))
        {
            ApplyHorizontalDelta(Input.GetAxisRaw("Mouse X"));
        }

        if (Input.GetMouseButtonUp(0))
        {
            mouseDragging = false;
        }
    }

    private bool IsPositionOverUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        pointerEventData ??= new PointerEventData(EventSystem.current);
        pointerEventData.position = screenPosition;
        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, uiRaycastResults);
        return uiRaycastResults.Count > 0;
    }

    private void ApplyHorizontalDelta(float horizontalDelta)
    {
        orbitalFollow.HorizontalAxis.Value += horizontalDelta * swipeSensitivity;
    }
}
