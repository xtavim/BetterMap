using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BetterMap.Scripts.Pins
{
    public static class PinLegend
    {
        private static readonly PinCategory[] Ours =
        {
            PinCategory.Ore,
            PinCategory.Forage,
            PinCategory.Dungeon,
            PinCategory.Loot,
            PinCategory.Spawner,
            PinCategory.Vegvisir,
            PinCategory.Beehive,
            PinCategory.Tar,
            PinCategory.Sap
        };

        public static Minimap.PinType CreatureType { get; private set; } = Minimap.PinType.Icon3;

        public static Minimap.PinType VehicleType { get; private set; } = Minimap.PinType.Icon0;

        private const float Margin = 10f;
        private const float Gap = 6f;

        private static readonly AccessTools.FieldRef<Minimap, bool[]> VisibleIconTypes =
            AccessTools.FieldRefAccess<Minimap, bool[]>("m_visibleIconTypes");

        private static readonly AccessTools.FieldRef<Minimap, Dictionary<Minimap.PinType, Image>> SelectedIcons =
            AccessTools.FieldRefAccess<Minimap, Dictionary<Minimap.PinType, Image>>("m_selectedIcons");

        private static readonly Dictionary<PinCategory, Minimap.PinType> _types =
            new Dictionary<PinCategory, Minimap.PinType>();

        private static readonly Dictionary<Minimap.PinType, PinCategory> _categories =
            new Dictionary<Minimap.PinType, PinCategory>();

        private static Minimap _builtInto;

        public static Minimap.PinType TypeOf(PinCategory category)
        {
            return _types.TryGetValue(category, out var type) ? type : Plugin.AutoPinType;
        }

        public static bool CategoryOf(Minimap.PinType type, out PinCategory category)
        {
            return _categories.TryGetValue(type, out category);
        }

        public static void Install()
        {
            var map = Minimap.instance;
            if (map == null || _builtInto == map) return;

            _builtInto = map;

            _types.Clear();
            _categories.Clear();

            CreatureType = Minimap.PinType.Icon3;
            VehicleType = Minimap.PinType.Icon0;

            try
            {
                Build(map);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(
                    $"PinLegend: could not add our pins to the legend, they will share a plain pin instead: {e}");

                _types.Clear();
                _categories.Clear();
            }
        }

        private static void Build(Minimap map)
        {
            var template = Parent(map.m_selectedIcon4);
            var firstIcon = Parent(map.m_selectedIcon0);

            if (template == null || firstIcon == null || template.parent == null)
            {
                Plugin.Logger.LogWarning("PinLegend: the map legend does not look the way it used to, leaving it alone");
                return;
            }

            var vanilla = template.parent as RectTransform;
            var death = Parent(map.m_selectedIconDeath);
            var extras = death != null ? death.parent as RectTransform : null;

            var first = Enum.GetValues(typeof(Minimap.PinType)).Length;
            Grow(map, first + Ours.Length + 2);

            var step = Step(map);

            Stretch(vanilla, step, 5 + Ours.Length);

            for (var i = 0; i < Ours.Length; i++)
            {
                var category = Ours[i];
                var type = (Minimap.PinType)(first + i);

                var sprite = Icons.For(category);
                if (sprite == null) continue;

                map.m_icons.Add(new Minimap.SpriteData { m_name = type, m_icon = sprite });

                var button = Clone(map, template, vanilla, type, sprite, category);
                if (button == null) continue;

                button.anchoredPosition = firstIcon.anchoredPosition + step * (5 + i);

                _types[category] = type;
                _categories[type] = category;
            }

            Unlisted(map, first + Ours.Length);

            Arrange(map, vanilla, extras);

            Plugin.Logger.LogInfo($"PinLegend: {_types.Count} of our pins added to the legend");
        }

        private static void Arrange(Minimap map, RectTransform vanilla, RectTransform extras)
        {
            if (vanilla == null) return;

            TopRight(vanilla);
            vanilla.anchoredPosition = new Vector2(-Margin, -Margin);

            if (extras != null)
            {
                TopRight(extras);
                extras.anchoredPosition = new Vector2(-Margin - vanilla.rect.width - Gap, -Margin);
            }

            var biome = map.m_biomeNameLarge != null ? map.m_biomeNameLarge.rectTransform : null;

            if (biome != null)
            {
                biome.anchorMin = new Vector2(0.5f, 1f);
                biome.anchorMax = new Vector2(0.5f, 1f);
                biome.pivot = new Vector2(0.5f, 1f);
                biome.anchoredPosition = new Vector2(0f, -Margin);

                map.m_biomeNameLarge.alignment = TMPro.TextAlignmentOptions.Top;
            }
        }

        private static void TopRight(RectTransform rect)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
        }

        // No button on purpose: whether creatures and vehicles are drawn is a setting, not
        // something the map's own filter should reach.
        private static void Unlisted(Minimap map, int first)
        {
            var fallback = map.m_icons.Find(x => x.m_name == Minimap.PinType.Icon3).m_icon;

            CreatureType = (Minimap.PinType)first;
            VehicleType = (Minimap.PinType)(first + 1);

            map.m_icons.Add(new Minimap.SpriteData { m_name = CreatureType, m_icon = fallback });
            map.m_icons.Add(new Minimap.SpriteData { m_name = VehicleType, m_icon = fallback });
        }

        private static void Stretch(RectTransform panel, Vector2 step, int slots)
        {
            if (panel == null) return;

            var run = Mathf.Abs(step.y);
            if (run <= 0f) return;

            var padding = panel.rect.height - run * 5f;

            panel.sizeDelta = new Vector2(panel.sizeDelta.x, run * slots + padding);
        }

        // Built to the length of the enum and indexed straight into, so it has to be long enough
        // before a larger type is ever used. AddPin quietly turns anything past it into a plain pin.
        private static void Grow(Minimap map, int length)
        {
            var visible = VisibleIconTypes(map);
            if (visible != null && visible.Length >= length) return;

            var grown = new bool[length];

            for (var i = 0; i < grown.Length; i++)
            {
                grown[i] = visible == null || i >= visible.Length || visible[i];
            }

            VisibleIconTypes(map) = grown;
        }

        private static Vector2 Step(Minimap map)
        {
            var third = Parent(map.m_selectedIcon3);
            var fourth = Parent(map.m_selectedIcon4);

            if (third == null || fourth == null) return new Vector2(0f, -56f);

            return fourth.anchoredPosition - third.anchoredPosition;
        }

        private static RectTransform Clone(Minimap map, Transform template, Transform panel,
            Minimap.PinType type, Sprite sprite, PinCategory category)
        {
            if (panel == null) return null;

            var clone = UnityEngine.Object.Instantiate(template.gameObject, panel);
            clone.name = "BetterMap_" + category;

            var icon = clone.GetComponent<Image>();
            if (icon != null) icon.sprite = sprite;

            var button = clone.GetComponent<Button>();
            if (button != null)
            {
                Silence(button.onClick);
                button.onClick.AddListener(() => Pressed(type));
            }

            var mouse = clone.GetComponent<MouseClick>();
            if (mouse != null)
            {
                Silence(mouse.m_leftClick);
                Silence(mouse.m_middleClick);
                Silence(mouse.m_rightClick);

                mouse.m_rightClick.AddListener(() => Filter(type));
            }

            var selected = clone.transform.Find("Selected");

            if (selected != null)
            {
                var image = selected.GetComponent<Image>();

                if (image != null)
                {
                    image.enabled = false;
                    SelectedIcons(map)[type] = image;
                }
            }

            return clone.transform as RectTransform;
        }

        private static RectTransform Parent(Image image)
        {
            return image != null ? image.transform.parent as RectTransform : null;
        }

        // Listeners set in the prefab cannot be removed at runtime, only switched off.
        private static void Silence(UnityEventBase e)
        {
            for (var i = 0; i < e.GetPersistentEventCount(); i++)
            {
                e.SetPersistentListenerState(i, UnityEventCallState.Off);
            }
        }

        private static readonly Action<Minimap, Minimap.PinType> IconPressed =
            AccessTools.MethodDelegate<Action<Minimap, Minimap.PinType>>(
                AccessTools.Method(typeof(Minimap), "IconPressed"), null, false);

        private static readonly Action<Minimap, Minimap.PinType> ToggleIconFilter =
            AccessTools.MethodDelegate<Action<Minimap, Minimap.PinType>>(
                AccessTools.Method(typeof(Minimap), "ToggleIconFilter"), null, false);

        private static void Pressed(Minimap.PinType type)
        {
            if (Minimap.instance != null) IconPressed(Minimap.instance, type);
        }

        private static void Filter(Minimap.PinType type)
        {
            if (Minimap.instance != null) ToggleIconFilter(Minimap.instance, type);
        }
    }
}
