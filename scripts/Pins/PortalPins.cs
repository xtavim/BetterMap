using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Pins
{
    public static class PortalPins
    {
        private static readonly AccessTools.FieldRef<Minimap, List<Minimap.SpriteData>> Icons =
            AccessTools.FieldRefAccess<Minimap, List<Minimap.SpriteData>>("m_icons");

        private static Minimap.PinType _type = Minimap.PinType.Icon4;
        private static bool _typeFound;

        private static HashSet<int> _prefabs;

        public static Minimap.PinType PinType => _type;

        public static string Label(string tag)
        {
            return string.IsNullOrEmpty(tag) ? "$piece_portal" : tag;
        }

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

        // Found by asking which type the map draws with a portal, rather than guessing among the
        // five numbered icons.
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
