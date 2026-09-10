using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Map
{
    [HarmonyPatch]
    public static class MapPatches
    {
        /// <summary>
        /// How far the map is uncovered around the player.
        ///
        /// Written here rather than once at startup because the value can change under us at any
        /// point: the config can be edited while the game runs, and on a server ServerSync pushes
        /// the host's value out after the map already exists. This is the only place the radius is
        /// read, so setting it here is always in time and never stale.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(Minimap), "UpdateExplore")]
        private static void Minimap_UpdateExplore_Prefix(Minimap __instance)
        {
            __instance.m_exploreRadius = Plugin.explorationRadius.Value;
        }

        /// <summary>
        /// Runs after the game has added its own death pin, which the rebuild then replaces with
        /// one that will still be there after a reload. The player has not moved yet: respawning
        /// is scheduled from here, not done.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Player), "OnDeath")]
        private static void Player_OnDeath_Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;

            DeathMarkers.Record(__instance.transform.position);
        }

        /// <summary>
        /// Loading map data clears every pin, ours included.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "SetMapData")]
        private static void Minimap_SetMapData_Postfix()
        {
            DeathMarkers.Invalidate();
        }
    }
}
