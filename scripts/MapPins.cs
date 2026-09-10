using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts
{
    public static class MapPins
    {
        // UpdatePins runs only when the map has been told something changed, so anything that
        // moves a pin has to say so or it steps instead of gliding.
        public static void RequestRedraw()
        {
            if (Minimap.instance != null) PinUpdateRequired(Minimap.instance) = true;
        }

        private static readonly AccessTools.FieldRef<Minimap, bool> PinUpdateRequired =
            AccessTools.FieldRefAccess<Minimap, bool>("m_pinUpdateRequired");

        private static readonly AccessTools.FieldRef<Minimap, List<Minimap.PinData>> Pins =
            AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>("m_pins");

        public static List<Minimap.PinData> Of(Minimap map)
        {
            return Pins(map);
        }

        public static void ApplyScale()
        {
            var map = Minimap.instance;
            if (map == null) return;

            var scale = Plugin.iconScale.Value;
            if (Mathf.Approximately(scale, 1f)) return;

            var normal = map.m_mode == Minimap.MapMode.Large ? map.m_pinSizeLarge : map.m_pinSizeSmall;

            foreach (var pin in Pins(map))
            {
                if (pin?.m_uiElement == null) continue;

                if (pin.m_worldSize > 0f) continue;

                var target = pin.m_animate
                    ? pin.m_uiElement.rect.width * scale
                    : (pin.m_doubleSize ? normal * 2f : normal) * scale;

                if (Mathf.Approximately(pin.m_uiElement.rect.width, target)) continue;

                pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, target);
                pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, target);
            }
        }
    }
}
