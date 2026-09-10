using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Pins
{
    /// <summary>
    /// Pins that carry a name they can only get from somewhere else.
    ///
    /// The game pins a trader itself and leaves the pin nameless, so a map with Haldor, Hildir and
    /// the bog witch on it says nothing about which is which. And a portal knows its tag, but only
    /// after someone has typed one.
    /// </summary>
    public static class NamedPins
    {
        private static readonly AccessTools.FieldRef<Minimap, Dictionary<Vector3, Minimap.PinData>> LocationPins =
            AccessTools.FieldRefAccess<Minimap, Dictionary<Vector3, Minimap.PinData>>("m_locationPins");

        /// <summary>
        /// The places the game pins by itself, and who is standing there. Every one of these tokens
        /// was checked against the game's own text before being written down.
        /// </summary>
        private static readonly Dictionary<string, string> Occupants = new Dictionary<string, string>
        {
            { "Vendor_BlackForest", "$npc_haldor" },
            { "Hildir_camp", "$npc_hildir" },
            { "BogWitch_Camp", "$npc_bogwitch" }
        };

        private static readonly Dictionary<Vector3, string> _icons = new Dictionary<Vector3, string>();

        /// <summary>
        /// Puts every trader in the world on the map, whether or not anyone has been there.
        ///
        /// Nothing here draws a pin. The game already pins a trader the moment its part of the world
        /// has been generated, through this list, and only waits on that. Letting the traders through
        /// early hands the job back to the game: its own icon, its own pin, sent to clients down its
        /// own channel, and named by the pass above.
        ///
        /// Only the server holds the list of what the world contains, which is the whole reason this
        /// can be answered at all. Playing alone that is you, so it works with nothing extra; a guest
        /// on a server without the mod sees no difference, because nobody there knows either.
        /// </summary>
        public static void RevealTraders(ZoneSystem zones, Dictionary<Vector3, string> icons)
        {
            if (!Plugin.revealTraders.Value) return;
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            if (zones.m_locationInstances == null) return;

            foreach (var instance in zones.m_locationInstances.Values)
            {
                var location = instance.m_location;
                if (location == null || !Occupants.ContainsKey(location.m_prefabName)) continue;

                icons[instance.m_position] = location.m_prefab.Name;
            }
        }

        /// <summary>
        /// Run after the game has rebuilt its own location pins, which it does every five seconds and
        /// which throws away and remakes any pin whose place has come or gone.
        /// </summary>
        public static void NameLocationPins()
        {
            if (!Plugin.nameTraderPins.Value) return;
            if (Minimap.instance == null || ZoneSystem.instance == null) return;

            var pins = LocationPins(Minimap.instance);
            if (pins.Count == 0) return;

            _icons.Clear();
            ZoneSystem.instance.GetLocationIcons(_icons);

            foreach (var pair in pins)
            {
                var pin = pair.Value;
                if (pin == null || !string.IsNullOrEmpty(pin.m_name)) continue;

                if (!_icons.TryGetValue(pair.Key, out var location)) continue;
                if (!Occupants.TryGetValue(location, out var occupant)) continue;

                pin.m_name = occupant;

                // The label is built by UpdatePins once this exists.
                pin.m_NamePinData = new Minimap.PinNameData(pin);

                MapPins.RequestRedraw();
            }
        }

        /// <summary>
        /// A portal's tag, once it has one.
        ///
        /// The pin cannot be edited: the game builds a pin's label the first time it draws it and
        /// never looks at the name again, so the pin standing there is replaced. The record is left
        /// alone, since the place is still a place we have pinned.
        /// </summary>
        public static void Retag(Vector3 position, string tag)
        {
            var map = Minimap.instance;
            if (map == null) return;

            var type = PortalPins.PinType;

            Minimap.PinData found = null;
            var nearest = float.MaxValue;

            foreach (var pin in MapPins.Of(map))
            {
                if (pin.m_type != type) continue;

                var distance = (pin.m_pos - position).sqrMagnitude;
                if (distance > 4f || distance >= nearest) continue;

                nearest = distance;
                found = pin;
            }

            if (found == null) return;

            map.RemovePin(found);

            var replacement = map.AddPin(found.m_pos, type, PortalPins.Label(tag),
                save: true, isChecked: false);

            replacement.m_NamePinData = new Minimap.PinNameData(replacement);

            if (Plugin.debugMode.Value) Plugin.Logger.LogInfo($"NamedPins: portal renamed to \"{tag}\"");
        }
    }

    [HarmonyPatch]
    public static class NamedPinPatches
    {
        /// <summary>
        /// Runs on the server, which is the only side where this list is built from the world rather
        /// than from what the server has already sent.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ZoneSystem), "GetLocationIcons")]
        private static void ZoneSystem_GetLocationIcons_Postfix(ZoneSystem __instance, Dictionary<Vector3, string> icons)
        {
            NamedPins.RevealTraders(__instance, icons);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdateLocationPins")]
        private static void Minimap_UpdateLocationPins_Postfix()
        {
            NamedPins.NameLocationPins();
        }

        /// <summary>
        /// Confirming the Set Tag screen. This is the call the text box makes, so it runs on the tag
        /// the player actually accepted rather than on every keystroke.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(TeleportWorld), "SetText")]
        private static void TeleportWorld_SetText_Postfix(TeleportWorld __instance, string text)
        {
            if (!Plugin.autoPinPortals.Value) return;

            NamedPins.Retag(__instance.transform.position, text);
        }
    }
}
