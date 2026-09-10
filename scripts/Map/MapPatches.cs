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
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdatePins")]
        private static void Minimap_UpdatePins_Postfix()
        {
            MapPins.ApplyScale();
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
