using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presents PlayerVitals in a small, low-contrast mobile HUD.
/// The views are generated once at runtime so future resource types can be
/// added without duplicating update logic across gameplay scripts.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class MobileVitalsHUD : MonoBehaviour
{
    [SerializeField] private PlayerVitals playerVitals;
    [SerializeField, Min(160f)] private float barWidth = 300f;
    [SerializeField, Min(16f)] private float barHeight = 26f;
    [SerializeField, Min(8f)] private float margin = 28f;
    [SerializeField, Min(0f)] private float spacing = 8f;

    private readonly Dictionary<PlayerResourceType, ResourceBarView> views = new();
    private RectTransform root;

    private void Awake()
    {
        if (playerVitals == null)
        {
            playerVitals = FindAnyObjectByType<PlayerVitals>();
        }

        if (playerVitals == null)
        {
            Debug.LogError($"{nameof(MobileVitalsHUD)} on {name} could not find {nameof(PlayerVitals)}.", this);
            enabled = false;
            return;
        }

        BuildViews();
        playerVitals.ResourceChanged += OnResourceChanged;
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (playerVitals != null)
        {
            playerVitals.ResourceChanged -= OnResourceChanged;
        }
    }

    private void BuildViews()
    {
        GameObject rootObject = new("VitalsBars");
        rootObject.transform.SetParent(transform, false);
        root = rootObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(margin, -margin);
        root.sizeDelta = new Vector2(barWidth, (barHeight + spacing) * 3f);
        root.localScale = Vector3.one;
        root.SetAsLastSibling();

        CreateView(PlayerResourceType.Health, "Health", new Color(0.68f, 0.29f, 0.31f, 0.9f), 0);
        CreateView(PlayerResourceType.Magic, "Magic", new Color(0.28f, 0.43f, 0.68f, 0.9f), 1);
        CreateView(PlayerResourceType.Stamina, "Stamina", new Color(0.44f, 0.62f, 0.35f, 0.9f), 2);
    }

    private void CreateView(PlayerResourceType resourceType, string label, Color fillColor, int index)
    {
        GameObject barObject = new(label + "Bar");
        barObject.transform.SetParent(root, false);

        RectTransform barTransform = barObject.AddComponent<RectTransform>();
        barTransform.anchorMin = new Vector2(0f, 1f);
        barTransform.anchorMax = new Vector2(0f, 1f);
        barTransform.pivot = new Vector2(0f, 1f);
        barTransform.anchoredPosition = new Vector2(0f, -index * (barHeight + spacing));
        barTransform.sizeDelta = new Vector2(barWidth, barHeight);

        Image background = barObject.AddComponent<Image>();
        background.color = new Color(0.06f, 0.07f, 0.08f, 0.65f);
        background.raycastTarget = false;

        GameObject fillObject = new("Fill");
        fillObject.transform.SetParent(barObject.transform, false);
        RectTransform fillTransform = fillObject.AddComponent<RectTransform>();
        fillTransform.anchorMin = new Vector2(0f, 0.5f);
        fillTransform.anchorMax = new Vector2(0f, 0.5f);
        fillTransform.pivot = new Vector2(0f, 0.5f);
        fillTransform.anchoredPosition = new Vector2(3f, 0f);
        fillTransform.sizeDelta = new Vector2(barWidth - 6f, barHeight - 6f);
        Image fill = fillObject.AddComponent<Image>();
        fill.color = fillColor;
        fill.raycastTarget = false;

        GameObject labelObject = new("Label");
        labelObject.transform.SetParent(barObject.transform, false);
        RectTransform labelTransform = labelObject.AddComponent<RectTransform>();
        labelTransform.anchorMin = Vector2.zero;
        labelTransform.anchorMax = Vector2.one;
        labelTransform.offsetMin = new Vector2(10f, 0f);
        labelTransform.offsetMax = new Vector2(-10f, 0f);
        Text text = labelObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = new Color(0.94f, 0.95f, 0.96f, 0.95f);
        text.raycastTarget = false;

        views.Add(
            resourceType,
            new ResourceBarView(label, fillTransform, fill, text, Mathf.Max(0f, barWidth - 6f)));
    }

    private void OnResourceChanged(PlayerResourceType resourceType, float current, float maximum)
    {
        if (views.TryGetValue(resourceType, out ResourceBarView view))
        {
            view.SetValue(current, maximum);
        }
    }

    private void RefreshAll()
    {
        foreach (KeyValuePair<PlayerResourceType, ResourceBarView> pair in views)
        {
            pair.Value.SetValue(
                playerVitals.GetCurrent(pair.Key),
                playerVitals.GetMaximum(pair.Key));
        }
    }

    private sealed class ResourceBarView
    {
        private readonly string label;
        private readonly RectTransform fillTransform;
        private readonly Image fill;
        private readonly Text text;
        private readonly float fillWidth;

        public ResourceBarView(
            string label,
            RectTransform fillTransform,
            Image fill,
            Text text,
            float fillWidth)
        {
            this.label = label;
            this.fillTransform = fillTransform;
            this.fill = fill;
            this.text = text;
            this.fillWidth = fillWidth;
        }

        public void SetValue(float current, float maximum)
        {
            float normalized = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            fillTransform.sizeDelta = new Vector2(fillWidth * normalized, fillTransform.sizeDelta.y);
            text.text = $"{label}  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
        }
    }
}
