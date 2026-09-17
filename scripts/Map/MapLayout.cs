using System.Collections.Generic;
using BetterMap.Scripts.Pins;
using UnityEngine;
using UnityEngine.UI;

namespace BetterMap.Scripts.Map
{
    public static class MapLayout
    {
        private const float Margin = 10f;
        private const float RowGap = 4f;
        private const float HintScale = 0.7f;
        private const float ToggleRow = 30f;
        private const float IconGap = 6f;

        private static readonly List<RectTransform> Rows = new List<RectTransform>();

        public static void Apply()
        {
            var map = Minimap.instance;
            if (map == null) return;

            Biome(map);
            Toggles(map);
            Hints(map);
        }

        private static void Biome(Minimap map)
        {
            if (map.m_biomeNameLarge == null) return;

            PinLegend.Anchor(map.m_biomeNameLarge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            map.m_biomeNameLarge.rectTransform.anchoredPosition = new Vector2(0f, -Margin);
            map.m_biomeNameLarge.alignment = TMPro.TextAlignmentOptions.Top;
        }

        // Moving a panel that turns out to hold the map takes the map off the screen with it.
        private static bool Carries(Transform candidate, Minimap map)
        {
            return Inside(candidate, map.m_mapImageLarge)
                   || Inside(candidate, map.m_pinRootLarge)
                   || Inside(candidate, map.m_pinNameRootLarge);
        }

        private static bool Inside(Transform candidate, Component thing)
        {
            return thing != null && thing.transform.IsChildOf(candidate);
        }

        private static void Toggles(Minimap map)
        {
            var root = map.m_largeRoot != null ? map.m_largeRoot.transform : null;
            if (root == null) return;

            Rows.Clear();

            foreach (var toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                if (toggle == null) continue;

                var row = toggle.transform.parent as RectTransform ?? toggle.transform as RectTransform;

                if (row == null || row == root || Rows.Contains(row)) continue;

                if (Carries(row, map))
                {
                    Plugin.Logger.LogWarning(
                        $"MapLayout: {Path(row)} holds the map itself, leaving the toggles where they are");

                    Rows.Clear();
                    return;
                }

                Rows.Add(row);
            }

            if (Rows.Count == 0) return;

            Rows.Sort((a, b) => a.anchoredPosition.y.CompareTo(b.anchoredPosition.y));

            var y = Margin;

            foreach (var row in Rows)
            {
                PinLegend.Anchor(row, Vector2.zero, Vector2.zero);
                row.anchoredPosition = new Vector2(Margin, y);

                y += (row.rect.height > 0f ? row.rect.height : ToggleRow) + RowGap;
            }

            Rows.Clear();
        }

        // Each bar holds one set of hints per kind of controller, stretched over each other with only
        // the one you are playing on shown, so the column belongs inside each set.
        private static void Hints(Minimap map)
        {
            if (map.m_hints == null) return;

            foreach (var hint in map.m_hints)
            {
                var bar = hint != null ? hint.transform as RectTransform : null;
                if (bar == null) continue;

                if (Carries(bar, map))
                {
                    Plugin.Logger.LogWarning(
                        $"MapLayout: {Path(bar)} holds the map itself, leaving the key hints where they are");
                    continue;
                }

                var height = 0f;

                for (var i = 0; i < bar.childCount; i++)
                {
                    if (bar.GetChild(i) is RectTransform variant) height = Mathf.Max(height, Column(variant));
                }

                // Only the vertical half of the anchoring. The bar is stretched across the width of
                // the map, and pinning it to a corner outright turns its inset into a width.
                bar.anchorMin = new Vector2(bar.anchorMin.x, 1f);
                bar.anchorMax = new Vector2(bar.anchorMax.x, 1f);
                bar.pivot = new Vector2(bar.pivot.x, 1f);

                bar.anchoredPosition = new Vector2(bar.anchoredPosition.x, -Margin);
                bar.sizeDelta = new Vector2(bar.sizeDelta.x, height);

                // Never scaled: a rect this wide shrinks toward its own middle, not its left edge.
                bar.localScale = Vector3.one;

                Flush(bar, Margin);
            }
        }

        private static float Column(RectTransform panel)
        {
            Strip(panel);

            var y = 0f;

            for (var i = 0; i < panel.childCount; i++)
            {
                var row = panel.GetChild(i) as RectTransform;
                if (row == null) continue;

                var height = row.rect.height * HintScale;

                Row(row);

                row.anchorMin = new Vector2(0f, 1f);
                row.anchorMax = new Vector2(0f, 1f);
                row.pivot = new Vector2(0f, 1f);
                row.localScale = Vector3.one * HintScale;

                row.anchoredPosition = new Vector2(0f, y);

                y -= height + RowGap;
            }

            Flush(panel, 0f);

            return -y;
        }

        private static void Row(RectTransform row)
        {
            Strip(row);

            RectTransform glyph = null;

            foreach (var image in row.GetComponentsInChildren<Image>(true))
            {
                if (image == null || image.transform.parent != row) continue;

                glyph = image.rectTransform;
                break;
            }

            if (glyph != null)
            {
                glyph.anchorMin = new Vector2(0f, 0.5f);
                glyph.anchorMax = new Vector2(0f, 0.5f);
                glyph.pivot = new Vector2(0f, 0.5f);
                glyph.anchoredPosition = Vector2.zero;
            }

            var label = row.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label == null) return;

            var rect = label.rectTransform;

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(glyph != null ? glyph.rect.width + IconGap : 0f, 0f);

            label.alignment = TMPro.TextAlignmentOptions.Left;
        }

        private static void Strip(Component holder)
        {
            var group = holder.GetComponent<LayoutGroup>();
            if (group != null) UnityEngine.Object.DestroyImmediate(group);

            var fitter = holder.GetComponent<ContentSizeFitter>();
            if (fitter != null) UnityEngine.Object.DestroyImmediate(fitter);
        }

        // A stretched rect keeps its size as an inset from each edge, not as a width.
        private static void Flush(RectTransform rect, float left)
        {
            if (Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x)) return;

            rect.offsetMin = new Vector2(left, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
        }

        private static string Path(Transform t)
        {
            var path = t.name;

            for (var parent = t.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;

            return path;
        }
    }
}
