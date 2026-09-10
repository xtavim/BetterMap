using HarmonyLib;

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
    }
}
