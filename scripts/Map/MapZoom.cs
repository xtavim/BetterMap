using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Map
{
    // Zoom input still goes through the game, but its setter is caught and turned into a target the
    // shown zoom glides toward, before the map is drawn in the same UpdateMap.
    [HarmonyPatch]
    public static class MapZoom
    {
        private const float Speed = 18f;

        private static readonly AccessTools.FieldRef<Minimap, float> LargeField =
            AccessTools.FieldRefAccess<Minimap, float>("m_largeZoom");

        private static readonly AccessTools.FieldRef<Minimap, float> SmallField =
            AccessTools.FieldRefAccess<Minimap, float>("m_smallZoom");

        private class Zoom
        {
            public float Target;
            public float Shown;
            public bool Ready;
        }

        private static readonly Zoom Large = new Zoom();
        private static readonly Zoom Small = new Zoom();

        private static bool _input;

        private static bool Active => Plugin.smoothZoom.Value && !ZInput.IsTouchActive();

        [HarmonyPrefix, HarmonyPatch(typeof(Minimap), "UpdateMap")]
        private static void Minimap_UpdateMap_Prefix(Minimap __instance, float dt)
        {
            if (!Active)
            {
                Large.Ready = false;
                Small.Ready = false;
                return;
            }

            var moved = Step(Large, ref LargeField(__instance), dt);
            moved |= Step(Small, ref SmallField(__instance), dt);

            if (moved) MapPins.RequestRedraw();

            _input = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdateMap")]
        private static void Minimap_UpdateMap_Postfix()
        {
            _input = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Minimap), nameof(Minimap.LargeZoom), MethodType.Setter)]
        private static void Minimap_LargeZoom_Prefix(Minimap __instance, ref float value)
        {
            Retarget(__instance, Large, ref value, LargeField(__instance));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Minimap), nameof(Minimap.SmallZoom), MethodType.Setter)]
        private static void Minimap_SmallZoom_Prefix(Minimap __instance, ref float value)
        {
            Retarget(__instance, Small, ref value, SmallField(__instance));
        }

        private static void Retarget(Minimap map, Zoom zoom, ref float value, float current)
        {
            if (!_input || !zoom.Ready || current <= 0f) return;

            zoom.Target = Mathf.Clamp(zoom.Target * (value / current), map.m_minZoom, map.m_maxZoom);
            value = current;
        }

        private static bool Step(Zoom zoom, ref float field, float dt)
        {
            // Anything other than our own glide moved it: follow instead of pulling it back.
            if (!zoom.Ready || !Mathf.Approximately(field, zoom.Shown))
            {
                zoom.Target = field;
                zoom.Shown = field;
                zoom.Ready = true;
                return false;
            }

            if (Mathf.Approximately(field, zoom.Target)) return false;

            var t = 1f - Mathf.Exp(-Speed * dt);
            var next = Mathf.Exp(Mathf.Lerp(Mathf.Log(field), Mathf.Log(zoom.Target), t));

            if (Mathf.Abs(next / zoom.Target - 1f) < 0.001f) next = zoom.Target;

            field = next;
            zoom.Shown = next;
            return true;
        }
    }
}
