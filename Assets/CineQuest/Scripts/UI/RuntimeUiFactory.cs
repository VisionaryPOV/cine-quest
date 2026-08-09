// Cine Quest — Shared runtime UI helpers (dark on-set theme).

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CineQuest.UI
{
    public static class RuntimeUiFactory
    {
        static Font _font;

        public static Font GetFont()
        {
            if (_font != null) return _font;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }

        public static Text CreateLabel(Transform parent, string name, string text, Vector2 anchoredPos,
            Vector2 size, int fontSize, TextAnchor anchor, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = GetFont();
            t.fontSize = fontSize;
            t.color = color ?? new Color(0.9f, 0.92f, 0.95f);
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return t;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size,
            UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.22f, 0.28f, 0.36f, 1f);
            colors.pressedColor = new Color(0.08f, 0.45f, 0.55f, 1f);
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var t = txtGo.AddComponent<Text>();
            t.text = label;
            t.font = GetFont();
            t.fontSize = Mathf.Max(12, (int)(size.y * 0.36f));
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            var trt = t.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            return btn;
        }

        /// <summary>
        /// Creates a labeled slider row. Returns (slider, value readout text).
        /// </summary>
        public static (Slider slider, Text valueText) CreateSliderRow(
            Transform parent, string name, string label, Vector2 pos, float min, float max, float value,
            UnityAction<float> onChanged)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.anchoredPosition = pos;
            rowRt.sizeDelta = new Vector2(720, 36);

            var labelT = CreateLabel(row.transform, "Label", label, new Vector2(-280, 0), new Vector2(140, 32),
                14, TextAnchor.MiddleLeft);

            var valueT = CreateLabel(row.transform, "Value", value.ToString("0.00"), new Vector2(300, 0),
                new Vector2(80, 32), 14, TextAnchor.MiddleRight, new Color(0.55f, 0.85f, 1f));

            // Track
            var trackGo = new GameObject("Track");
            trackGo.transform.SetParent(row.transform, false);
            var trackImg = trackGo.AddComponent<Image>();
            trackImg.color = new Color(0.08f, 0.09f, 0.12f, 1f);
            var trackRt = trackGo.GetComponent<RectTransform>();
            trackRt.anchoredPosition = new Vector2(40, 0);
            trackRt.sizeDelta = new Vector2(400, 10);

            var slider = trackGo.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.wholeNumbers = false;

            // Fill
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(trackGo.transform, false);
            var fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.offsetMin = Vector2.zero;
            fillAreaRt.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.55f, 0.75f, 1f);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            // Handle
            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(trackGo.transform, false);
            var handleAreaRt = handleArea.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(8, 0);
            handleAreaRt.offsetMax = new Vector2(-8, 0);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(0.85f, 0.9f, 0.95f, 1f);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(16, 20);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;

            slider.onValueChanged.AddListener(v =>
            {
                valueT.text = v.ToString("0.00");
                onChanged?.Invoke(v);
            });

            // Suppress unused warning for labelT
            _ = labelT;

            return (slider, valueT);
        }

        public static Toggle CreateToggle(Transform parent, string name, string label, Vector2 pos, bool isOn,
            UnityAction<bool> onChanged)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200, 36);

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.15f, 1f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.anchoredPosition = new Vector2(16, 0);
            bgRt.sizeDelta = new Vector2(28, 28);

            var check = new GameObject("Checkmark");
            check.transform.SetParent(bg.transform, false);
            var checkImg = check.AddComponent<Image>();
            checkImg.color = new Color(0.3f, 0.85f, 0.55f, 1f);
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.2f, 0.2f);
            checkRt.anchorMax = new Vector2(0.8f, 0.8f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;

            var labelT = CreateLabel(go.transform, "Label", label, new Vector2(90, 0), new Vector2(150, 32),
                14, TextAnchor.MiddleLeft);

            var toggle = go.AddComponent<Toggle>();
            toggle.graphic = checkImg;
            toggle.targetGraphic = bgImg;
            toggle.isOn = isOn;
            if (onChanged != null) toggle.onValueChanged.AddListener(onChanged);

            _ = labelT;
            return toggle;
        }
    }
}
