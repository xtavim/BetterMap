using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts
{
    public static class MapPins
    {
        /// <summary>
        /// Ask the map to draw its pins again.
        ///
        /// UpdatePins is not a per frame job: it runs only when the map has been told something
        /// changed. Moving the player sets that every frame, so anything that moves looks right
        /// while you walk and visibly steps the moment you stand still, at whatever rate the mod
        /// happens to touch the pin. Anything here that moves a pin says so, which is what the
        /// game's own player pins do.
        /// </summary>
        public static void RequestRedraw()
        {
            if (Minimap.instance != null) PinUpdateRequired(Minimap.instance) = true;
        }

        private static readonly AccessTools.FieldRef<Minimap, bool> PinUpdateRequired =
            AccessTools.FieldRefAccess<Minimap, bool>("m_pinUpdateRequired");

        private static readonly AccessTools.FieldRef<Minimap, List<Minimap.PinData>> Pins =
            AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>("m_pins");

        /// <summary>
        /// Resize every pin on the map, the game's as well as ours.
        ///
        /// Has to be done here, after UpdatePins. The game sizes a pin once, in the frame it builds
        /// the marker, and rebuilds that marker whenever the pin leaves the visible part of the map
        /// and comes back, so a size written anywhere else is undone the next time you pan away.
        /// </summary>
        /// <summary>Every pin currently on the map.</summary>
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

                // Pins measured in metres rather than pixels, the event circles. Scaling one would
                // draw a lie about how big the area is.
                if (pin.m_worldSize > 0f) continue;

                // A pulsing pin has just been given this frame's size by the game, so it is scaled
                // from that. Every other pin is scaled from the size it would have had.
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
