using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BetterMap.Scripts.Pins
{
    // The legend's right click hides a kind of pin. The game forgets that twice over: AddPin shows a
    // hidden kind again so a pin placed by hand is never invisible, which our sweep trips every time
    // it pins something, and the map starts every world with everything shown.
    [HarmonyPatch]
    public static class PinFilters
    {
        private const string CustomDataKey = "BetterMap.hidden";

        private static readonly AccessTools.FieldRef<Minimap, bool[]> VisibleIconTypes =
            AccessTools.FieldRefAccess<Minimap, bool[]>("m_visibleIconTypes");

        private static readonly AccessTools.FieldRef<Minimap, Dictionary<Minimap.PinType, Image>> SelectedIcons =
            AccessTools.FieldRefAccess<Minimap, Dictionary<Minimap.PinType, Image>>("m_selectedIcons");

        private static readonly AccessTools.FieldRef<Minimap, bool> PinUpdateRequired =
            AccessTools.FieldRefAccess<Minimap, bool>("m_pinUpdateRequired");

        // The map the saved filters were put back onto. Until then nothing is written, or the empty
        // filters of a map still loading would overwrite the saved ones.
        private static Minimap _restoredOnto;

        public static Minimap.PinData AddQuietly(Vector3 pos, Minimap.PinType type, string name)
        {
            var map = Minimap.instance;
            var hidden = !IsVisible(map, type);

            var pin = map.AddPin(pos, type, name, save: true, isChecked: false);

            if (hidden && IsVisible(map, type))
            {
                VisibleIconTypes(map)[(int)type] = false;
                Refresh(map);
                Store(map);
            }

            return pin;
        }

        public static void Tick()
        {
            var map = Minimap.instance;
            if (map == null || _restoredOnto == map || Player.m_localPlayer == null) return;

            _restoredOnto = map;

            var visible = VisibleIconTypes(map);
            if (visible == null) return;

            if (!Player.m_localPlayer.m_customData.TryGetValue(CustomDataKey, out var text)) return;
            if (string.IsNullOrEmpty(text)) return;

            foreach (var token in text.Split(','))
            {
                if (!TypeOf(token, out var type)) continue;
                if ((int)type < 0 || (int)type >= visible.Length) continue;

                visible[(int)type] = false;
            }

            Refresh(map);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "ToggleIconFilter")]
        private static void Minimap_ToggleIconFilter_Postfix(Minimap __instance)
        {
            Store(__instance);
        }

        private static void Store(Minimap map)
        {
            if (map == null || _restoredOnto != map || Player.m_localPlayer == null) return;

            var visible = VisibleIconTypes(map);
            if (visible == null) return;

            var hidden = new List<string>();

            for (var i = 0; i < visible.Length; i++)
            {
                if (visible[i]) continue;

                var token = TokenOf((Minimap.PinType)i);
                if (token != null) hidden.Add(token);
            }

            Player.m_localPlayer.m_customData[CustomDataKey] = string.Join(",", hidden.ToArray());
        }

        // Our kinds are saved by category, since the slot they get depends on which other mods took
        // slots first. The game's own are saved by name. Another mod's slot has neither and is left out.
        private static string TokenOf(Minimap.PinType type)
        {
            if (PinLegend.CategoryOf(type, out var category)) return "BetterMap." + category;

            return Enum.IsDefined(typeof(Minimap.PinType), type) ? type.ToString() : null;
        }

        private static bool TypeOf(string token, out Minimap.PinType type)
        {
            type = default;

            if (token.StartsWith("BetterMap.", StringComparison.Ordinal))
            {
                if (!Enum.TryParse(token.Substring("BetterMap.".Length), out PinCategory category)) return false;
                if (!PinLegend.HasType(category)) return false;

                type = PinLegend.TypeOf(category);
                return true;
            }

            return Enum.TryParse(token, out type) && Enum.IsDefined(typeof(Minimap.PinType), type);
        }

        private static bool IsVisible(Minimap map, Minimap.PinType type)
        {
            var visible = VisibleIconTypes(map);
            return visible == null || (int)type < 0 || (int)type >= visible.Length || visible[(int)type];
        }

        private static void Refresh(Minimap map)
        {
            PinUpdateRequired(map) = true;

            var visible = VisibleIconTypes(map);

            foreach (var pair in SelectedIcons(map))
            {
                if (pair.Value == null || (int)pair.Key >= visible.Length) continue;

                var button = pair.Value.transform.parent.GetComponent<Image>();
                if (button != null) button.color = visible[(int)pair.Key] ? Color.white : Color.gray;
            }
        }
    }
}
