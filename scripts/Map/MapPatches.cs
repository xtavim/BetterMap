using BetterMap.Scripts;
using HarmonyLib;

namespace BetterMap.Scripts.Map
{
    [HarmonyPatch]
    public static class MapPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(Minimap), "UpdateExplore")]
        private static void Minimap_UpdateExplore_Prefix(Minimap __instance)
        {
            __instance.m_exploreRadius = Plugin.explorationRadius.Value;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "Start")]
        private static void Minimap_Start_Postfix()
        {
            Pins.PinLegend.Install();
            MapLayout.Apply();
        }

        // Measured again on every open, since the window can be resized between one look and the next.
        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "SetMapMode")]
        private static void Minimap_SetMapMode_Postfix(Minimap.MapMode mode)
        {
            if (mode == Minimap.MapMode.Large) Pins.PinLegend.Rescale();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdatePins")]
        private static void Minimap_UpdatePins_Postfix()
        {
            MapPins.ApplyScale();
        }

        // Death markers are drawn unsaved, and the game only offers saved pins for removal, so a right
        // click passes through them. Ours are taken only when the game turned nothing down.
        [HarmonyPrefix, HarmonyPatch(typeof(Minimap), "RemovePinUnderPointer")]
        private static void Minimap_RemovePinUnderPointer_Prefix(Minimap __instance, out int __state)
        {
            __state = MapPins.Of(__instance).Count;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "RemovePinUnderPointer")]
        private static void Minimap_RemovePinUnderPointer_Postfix(Minimap __instance, int __state)
        {
            if (MapPins.Of(__instance).Count != __state) return;

            DeathMarkers.RemoveUnderPointer(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "OnDeath")]
        private static void Player_OnDeath_Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;

            DeathMarkers.Record(__instance.transform.position);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "SetMapData")]
        private static void Minimap_SetMapData_Postfix()
        {
            DeathMarkers.Invalidate();
            Pins.AutoPins.Restore();
        }
    }
}
