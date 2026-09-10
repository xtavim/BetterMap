using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Pins
{
    /// <summary>
    /// Portals, pinned where they stand and named by their tag.
    ///
    /// The one thing here that is not ours: the icon. The game already has a portal among its pin
    /// types, so a portal pin is saved as that type and comes back from the save wearing the right
    /// picture without anything being put back. Which type it is is found by looking for it rather
    /// than written down, because the answer is in the map's own icon table.
    /// </summary>
    public static class PortalPins
    {
        private static readonly AccessTools.FieldRef<Minimap, List<Minimap.SpriteData>> Icons =
            AccessTools.FieldRefAccess<Minimap, List<Minimap.SpriteData>>("m_icons");

        private static Minimap.PinType _type = Minimap.PinType.Icon4;
        private static bool _typeFound;

        private static HashSet<int> _prefabs;

        public static Minimap.PinType PinType => _type;

        /// <summary>
        /// A portal with no tag still deserves a pin, and calling it nothing would leave the map
        /// showing a picture with no word under it.
        /// </summary>
        public static string Label(string tag)
        {
            return string.IsNullOrEmpty(tag) ? "$piece_portal" : tag;
        }

        /// <summary>
        /// Which prefabs are portals, taken from the component rather than a list of names, so one
        /// added by an update or another mod is picked up without anything here changing.
        /// </summary>
        public static bool IsPortal(int prefab)
        {
            if (_prefabs == null)
            {
                var scene = ZNetScene.instance;
                if (scene == null || scene.m_prefabs == null) return false;

                _prefabs = new HashSet<int>();

                foreach (var prefab1 in scene.m_prefabs)
                {
                    if (prefab1 == null || prefab1.GetComponent<TeleportWorld>() == null) continue;

                    _prefabs.Add(prefab1.name.GetStableHashCode());
                }

                FindPinType();
            }

            return _prefabs.Contains(prefab);
        }

        /// <summary>
        /// The map's icon table pairs a pin type with the sprite drawn for it, so the portal type is
        /// whichever one is drawn with the portal. Guessing at the five numbered icons would be a
        /// guess; this is the map answering for itself.
        /// </summary>
        private static void FindPinType()
        {
            if (_typeFound || Minimap.instance == null) return;

            _typeFound = true;

            foreach (var entry in Icons(Minimap.instance))
            {
                if (entry.m_icon == null) continue;
                if (entry.m_icon.name.IndexOf("portal", StringComparison.OrdinalIgnoreCase) < 0) continue;

                _type = entry.m_name;

                if (Plugin.debugMode.Value)
                {
                    Plugin.Logger.LogInfo($"PortalPins: portals go on {_type}, drawn as {entry.m_icon.name}");
                }

                return;
            }

            Plugin.Logger.LogWarning(
                $"PortalPins: no pin type is drawn with a portal, so portals will use {_type}. Their icon will be whatever that type carries.");
        }

        public static string TagOf(ZDO zdo)
        {
            return zdo.GetString(ZDOVars.s_tag, "");
        }
    }
}
