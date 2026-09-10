using HarmonyLib;

namespace BetterMap.Scripts.Creatures
{
    [HarmonyPatch]
    public static class CreaturePatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdatePins")]
        private static void Minimap_UpdatePins_Postfix()
        {
            CreatureTracker.RestylePins();
        }
    }
}
