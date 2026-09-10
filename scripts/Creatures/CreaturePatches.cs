using HarmonyLib;

namespace BetterMap.Scripts.Creatures
{
    [HarmonyPatch]
    public static class CreaturePatches
    {
        /// <summary>
        /// UpdatePins sets every pin's icon colour on every frame, white for our own pins, so a tint
        /// applied once is gone by the next frame. Reapplying it here is the only place it sticks.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdatePins")]
        private static void Minimap_UpdatePins_Postfix()
        {
            CreatureTracker.ApplyTints();
        }
    }
}
