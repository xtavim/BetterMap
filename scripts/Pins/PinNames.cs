using System.Collections.Generic;
using UnityEngine;

namespace BetterMap.Scripts.Pins
{
    public static class PinNames
    {
        private static Dictionary<string, PinCategory> _categories;

        // The name is what identifies a pin as ours after a reload, which is safe because Valheim
        // cannot rename one: placing a pin makes a new one, clicking an existing one only ticks it.
        public static string For(PinRules.Rule rule, GameObject prefab)
        {
            if (!string.IsNullOrEmpty(rule.NameToken)) return rule.NameToken;

            return For(prefab) ?? rule.Name;
        }

        public static string For(GameObject prefab)
        {
            if (prefab == null) return null;

            var piece = prefab.GetComponent<Piece>();
            if (piece != null && !string.IsNullOrEmpty(piece.m_name)) return piece.m_name;

            var pickable = prefab.GetComponent<Pickable>();
            if (pickable?.m_itemPrefab != null)
            {
                var name = ItemName(pickable.m_itemPrefab);
                if (name != null) return name;
            }

            var mineRock = prefab.GetComponent<MineRock>();
            if (mineRock != null)
            {
                var name = DropName(mineRock.m_dropItems);
                if (name != null) return name;
            }

            var mineRock5 = prefab.GetComponent<MineRock5>();
            if (mineRock5 != null)
            {
                var name = DropName(mineRock5.m_dropItems);
                if (name != null) return name;
            }

            var destroyed = prefab.GetComponent<DropOnDestroyed>();
            if (destroyed?.m_dropWhenDestroyed != null)
            {
                var name = DropName(destroyed.m_dropWhenDestroyed);
                if (name != null) return name;
            }

            var destructible = prefab.GetComponent<Destructible>();
            if (destructible?.m_spawnWhenDestroyed != null) return For(destructible.m_spawnWhenDestroyed);

            var hover = prefab.GetComponent<HoverText>();
            if (hover != null && !string.IsNullOrEmpty(hover.m_text)) return hover.m_text;

            return null;
        }

        public static bool Category(string name, out PinCategory category)
        {
            Build();

            category = PinCategory.Ore;

            return !string.IsNullOrEmpty(name) && _categories != null && _categories.TryGetValue(name, out category);
        }

        public static void Build()
        {
            if (_categories != null) return;

            var scene = ZNetScene.instance;
            if (scene == null) return;

            var categories = new Dictionary<string, PinCategory>();

            foreach (var rule in PinRules.All)
            {
                foreach (var prefabName in rule.Prefabs)
                {
                    var prefab = rule.IsLocation ? null : scene.GetPrefab(prefabName);
                    if (prefab == null && !rule.IsLocation) continue;

                    var name = For(rule, prefab);

                    categories[name] = rule.Category;
                }
            }

            _categories = categories;

            if (Plugin.debugMode.Value) Plugin.Logger.LogInfo($"PinNames: {categories.Count} names known");
        }

        private static string ItemName(GameObject item)
        {
            var drop = item.GetComponent<ItemDrop>();
            var shared = drop?.m_itemData?.m_shared;

            return string.IsNullOrEmpty(shared?.m_name) ? null : shared.m_name;
        }

        private static readonly HashSet<string> Filler = new HashSet<string>
        {
            "Stone", "Wood", "RoundLog", "FineWood", "ElderBark", "YggdrasilWood", "Grausten"
        };

        // A deposit drops the rock it was buried in first, which named every copper deposit Stone.
        private static string DropName(DropTable table)
        {
            if (table?.m_drops == null) return null;

            string filler = null;

            foreach (var drop in table.m_drops)
            {
                if (drop.m_item == null) continue;

                var name = ItemName(drop.m_item);
                if (name == null) continue;

                if (Filler.Contains(drop.m_item.name))
                {
                    if (filler == null) filler = name;
                    continue;
                }

                return name;
            }

            return filler;
        }
    }
}
