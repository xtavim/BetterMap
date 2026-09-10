using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Vehicles
{
    [HarmonyPatch]
    public static class VehiclePatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdatePins")]
        private static void Minimap_UpdatePins_Postfix()
        {
            VehicleTracker.RestylePins();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdatePlayerMarker")]
        private static void Minimap_UpdatePlayerMarker_Postfix(Minimap __instance)
        {
            if (!Plugin.showBoats.Value) return;

            if (__instance.m_smallShipMarker != null)
                __instance.m_smallShipMarker.gameObject.SetActive(false);

            if (__instance.m_largeShipMarker != null)
                __instance.m_largeShipMarker.gameObject.SetActive(false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "StartDoodadControl")]
        private static void Player_StartDoodadControl_Postfix(Player __instance, IDoodadController shipControl)
        {
            if (__instance != Player.m_localPlayer || shipControl == null) return;

            Remember(shipControl.GetControlledComponent());
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Vagon), "AttachTo")]
        private static void Vagon_AttachTo_Postfix(Vagon __instance, GameObject go)
        {
            if (Player.m_localPlayer == null || go != Player.m_localPlayer.gameObject) return;

            Remember(__instance);
        }

        private static void Remember(Component component)
        {
            if (component == null) return;

            var nview = component.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;

            if (VehicleKinds.Get(nview.GetZDO().GetPrefab()) == null) return;

            VehicleMemory.Remember(nview.GetZDO().m_uid);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Game), "Start")]
        private static void Game_Start_Postfix()
        {
            VehicleRpc.Register();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Game), "Logout")]
        private static void Game_Logout_Postfix()
        {
            VehicleRpc.Reset();
            VehicleMemory.Forget();
        }
    }
}
