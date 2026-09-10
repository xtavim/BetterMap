using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Pins
{
    public static class NamedPins
    {
        private static readonly AccessTools.FieldRef<Minimap, Dictionary<Vector3, Minimap.PinData>> LocationPins =
            AccessTools.FieldRefAccess<Minimap, Dictionary<Vector3, Minimap.PinData>>("m_locationPins");

        private static readonly Dictionary<string, string> Occupants = new Dictionary<string, string>
        {
            { "Vendor_BlackForest", "$npc_haldor" },
            { "Hildir_camp", "$npc_hildir" },
            { "BogWitch_Camp", "$npc_bogwitch" }
        };

        private static readonly Dictionary<Vector3, string> _icons = new Dictionary<Vector3, string>();

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

                pin.m_NamePinData = new Minimap.PinNameData(pin);

                MapPins.RequestRedraw();
            }
        }

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

        [HarmonyPostfix, HarmonyPatch(typeof(TeleportWorld), "SetText")]
        private static void TeleportWorld_SetText_Postfix(TeleportWorld __instance, string text)
        {
            if (!Plugin.autoPinPortals.Value) return;

            NamedPins.Retag(__instance.transform.position, text);
        }
    }
}
