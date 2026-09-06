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
    private Vector2 previousMousePosition;
    private PlayerCombatTargeting targeting;

    private void Awake()
    {
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        targeting = FindFirstObjectByType<PlayerCombatTargeting>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) { OnDisable(); return; }
        ReadTouchSwipe();

        if (Input.touchCount == 0)
        {
            ReadMouseDrag();
        }
        if (targeting != null && targeting.IsLocked && activeFingerId < 0 && !mouseDragging)
        {
            Vector3 facing = targeting.FacingDirection;
            float heading = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
            orbitalFollow.HorizontalAxis.Value = Mathf.LerpAngle(orbitalFollow.HorizontalAxis.Value, heading, 1f - Mathf.Exp(-3f * Time.deltaTime));
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

        if (Input.GetMouseButtonDown(1))
        {
            mouseDragging = !IsPositionOverUi(mousePosition);
            previousMousePosition = mousePosition;
        }

        if (mouseDragging && Input.GetMouseButton(1))
        {
            ApplyHorizontalDelta(mousePosition.x - previousMousePosition.x);
            previousMousePosition = mousePosition;
        }

        if (!Input.GetMouseButton(1))
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
        orbitalFollow.HorizontalAxis.Value += horizontalDelta * swipeSensitivity * 1080f / Mathf.Max(1, Screen.height);
    }

    private void OnApplicationFocus(bool focused) { if (!focused) OnDisable(); }
    private void OnApplicationPause(bool paused) { if (paused) OnDisable(); }
}
