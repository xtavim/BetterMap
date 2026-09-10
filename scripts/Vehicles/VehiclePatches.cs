using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Vehicles
{
    [HarmonyPatch]
    public static class VehiclePatches
    {
        /// <summary>
        /// The only point at which a vehicle pin can be turned to its heading: UpdatePins rebuilds
        /// the marker from the pin data, which carries no angle.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdatePins")]
        private static void Minimap_UpdatePins_Postfix()
        {
            VehicleTracker.RestylePins();
        }

        /// <summary>
        /// The game puts its own boat marker on the map while you are sailing, a separate piece of
        /// UI rather than a pin. Ours is already on that boat and already turned to its heading, so
        /// the two sit on top of each other in different styles. This hides the game's, leaving one
        /// boat that looks the same whether you are aboard it or looking for it.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Minimap), "UpdatePlayerMarker")]
        private static void Minimap_UpdatePlayerMarker_Postfix(Minimap __instance)
        {
            if (!Plugin.showBoats.Value) return;

            if (__instance.m_smallShipMarker != null)
                __instance.m_smallShipMarker.gameObject.SetActive(false);

            if (__instance.m_largeShipMarker != null)
                __instance.m_largeShipMarker.gameObject.SetActive(false);
        }

        /// <summary>
        /// Taking the helm. This fires once control is actually granted, and only for whoever is
        /// steering, so a passenger does not come away with a permanent marker on someone else's
        /// boat.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Player), "StartDoodadControl")]
        private static void Player_StartDoodadControl_Postfix(Player __instance, IDoodadController shipControl)
        {
            if (__instance != Player.m_localPlayer || shipControl == null) return;

            Remember(shipControl.GetControlledComponent());
        }

        /// <summary>
        /// Picking up a cart. Vagon.Interact only asks the owner for permission and always reports
        /// failure, so it says nothing about whether the cart was taken; this runs when it actually
        /// is.
        /// </summary>
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

        /// <summary>
        /// Both sides register the same pair of calls. A dedicated server gets here too, which is
        /// what lets it answer.
        /// </summary>
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
