using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterMap.Scripts.Creatures
{
    public static class CreatureStars
    {
        private const string ChildName = "BetterMap_Stars";

        private static Sprite _star;
        private static TMP_FontAsset _font;
        private static Material _material;
        private static Color _color;

        private static readonly Color Gold = new Color(1f, 0.8f, 0.2f, 1f);

        private static bool _gaveUp;

        // The badge hangs off the pin's marker, which the map destroys and rebuilds whenever the pin
        // leaves view or the map changes size, so this is asked again on every redraw.
        public static void Apply(Minimap.PinData pin, int level)
        {
            if (pin?.m_uiElement == null) return;

            var existing = pin.m_uiElement.Find(ChildName);
            var stars = level - 1;

            if (stars < 1 || !Plugin.showCreatureStars.Value)
            {
                if (existing != null) Object.Destroy(existing.gameObject);
                return;
            }

            if (existing != null || !Borrow()) return;

            Build(pin.m_uiElement, stars);
        }

        private static bool Borrow()
        {
            if (_star != null && _font != null) return true;
            if (_gaveUp) return false;

            var hud = EnemyHud.instance;
            if (hud == null || hud.m_baseHud == null) return false;

            var level = hud.m_baseHud.transform.Find("level_2");
            var image = level != null ? level.GetComponentInChildren<Image>(true) : null;

            var name = hud.m_baseHud.transform.Find("Name");
            var text = name != null ? name.GetComponent<TextMeshProUGUI>() : null;

            if (image == null || image.sprite == null || text == null || text.font == null)
            {
                _gaveUp = true;
                Plugin.Logger.LogWarning("CreatureStars: the creature health bar does not look the way it used to, no stars on the map");
                return false;
            }

            _star = image.sprite;
            _font = text.font;
            _material = text.fontSharedMaterial;

            Color.RGBToHSV(image.color, out _, out var saturation, out _);
            _color = saturation > 0.3f ? image.color : Gold;

            return true;
        }

        private static void Build(RectTransform marker, int stars)
        {
            var root = new GameObject(ChildName, typeof(RectTransform)).transform as RectTransform;
            root.SetParent(marker, false);

            root.anchorMin = new Vector2(0.1f, 0.95f);
            root.anchorMax = new Vector2(0.9f, 1.45f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var star = new GameObject("Star", typeof(RectTransform), typeof(Image));
            var starRect = star.transform as RectTransform;
            starRect.SetParent(root, false);
            starRect.anchorMin = new Vector2(0.15f, 0f);
            starRect.anchorMax = new Vector2(0.5f, 1f);
            starRect.offsetMin = Vector2.zero;
            starRect.offsetMax = Vector2.zero;

            var image = star.GetComponent<Image>();
            image.sprite = _star;
            image.color = _color;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var count = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            var countRect = count.transform as RectTransform;
            countRect.SetParent(root, false);
            countRect.anchorMin = new Vector2(0.52f, 0f);
            countRect.anchorMax = new Vector2(0.95f, 1f);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;

            var text = count.GetComponent<TextMeshProUGUI>();
            text.font = _font;
            if (_material != null) text.fontSharedMaterial = _material;
            text.text = stars.ToString();
            text.enableAutoSizing = true;
            text.fontSizeMin = 1f;
            text.fontSizeMax = 72f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = Color.white;
            text.raycastTarget = false;
        }
    }
}
