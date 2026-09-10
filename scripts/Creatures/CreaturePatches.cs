using HarmonyLib;

namespace BetterMap.Scripts.Creatures
{
    [HarmonyPatch]
    public static class CreaturePatches
    {
        /// <summary>
        /// The only point at which creature pins can be styled: UpdatePins decides size and colour
        /// itself, so anything set earlier is overwritten by the time the pin is drawn.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdatePins")]
        private static void Minimap_UpdatePins_Postfix()
        {
            CreatureTracker.RestylePins();
        }
    }
}
