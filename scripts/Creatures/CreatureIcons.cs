using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterMap.Scripts.Creatures
{
    public static class CreatureIcons
    {
        private static readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>();

        public static Sprite Get(Character creature)
        {
            if (creature == null) return null;

            var key = PrefabName(creature.gameObject);

            if (_icons.TryGetValue(key, out var cached)) return cached;

            var icon = Resolve(creature.gameObject);
            _icons[key] = icon;

            if (Plugin.debugMode.Value)
            {
                Plugin.Logger.LogInfo($"CreatureIcons: {key} -> {(icon != null ? icon.name : "no trophy")}");
            }

            return icon;
        }

        private static Sprite Resolve(GameObject prefab)
        {
            var drops = prefab.GetComponent<CharacterDrop>();
            if (drops?.m_drops == null) return null;

            foreach (var drop in drops.m_drops)
            {
                if (drop?.m_prefab == null) continue;
                if (!drop.m_prefab.name.StartsWith("Trophy", StringComparison.OrdinalIgnoreCase)) continue;

                var item = drop.m_prefab.GetComponent<ItemDrop>();
                var icons = item?.m_itemData?.m_shared?.m_icons;

                if (icons != null && icons.Length > 0) return icons[0];
            }

            return null;
        }

        public static string PrefabName(GameObject go)
        {
            var name = go.name;
            var clone = name.IndexOf("(Clone)", StringComparison.Ordinal);
            return clone < 0 ? name : name.Substring(0, clone);
        }

        public static void Clear()
        {
            _icons.Clear();
        }
    }
}
