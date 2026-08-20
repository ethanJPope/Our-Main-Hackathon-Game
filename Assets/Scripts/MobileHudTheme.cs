using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the shared low-contrast mobile-control palette at runtime.
/// Keeping the palette in one component prevents joystick and action-button
/// styling from drifting apart as the HUD grows.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class MobileHudTheme : MonoBehaviour
{
    private static readonly Color ControlShadow = new(0.03f, 0.04f, 0.05f, 0.55f);
    private static readonly Color ControlOuter = new(0.18f, 0.19f, 0.2f, 0.35f);
    private static readonly Color ControlInner = new(0.14f, 0.15f, 0.16f, 0.45f);
    private static readonly Color ControlKnob = new(0.35f, 0.37f, 0.39f, 0.65f);
    private static readonly Color ControlAccent = new(0.7f, 0.72f, 0.74f, 0.7f);
    private static readonly Color ControlText = new(0.86f, 0.88f, 0.9f, 0.82f);

    private void Awake()
    {
        ApplyControlPalette(transform);
    }

    private void OnEnable()
    {
        ApplyControlPalette(transform);
    }

    private void OnValidate()
    {
        ApplyControlPalette(transform);
    }

    public static void ApplyControlPalette(Transform root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            image.color = image.name switch
            {
                "BaseShadow" or "Shadow" or "KnobShadow" => ControlShadow,
                "BaseOuter" or "Outer" => ControlOuter,
                "BaseInner" or "Inner" => ControlInner,
                "KnobInner" or "Highlight" => ControlKnob,
                "KnobAccent" or "GoldRim" => ControlAccent,
                _ => image.color
            };
        }

        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            if (text.name is "Label" or "DodgeArrow")
            {
                text.color = ControlText;
            }
        }
    }
}
