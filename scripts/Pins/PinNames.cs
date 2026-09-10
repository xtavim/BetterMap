using System.Collections.Generic;
using UnityEngine;

namespace BetterMap.Scripts.Pins
{
    /// <summary>
    /// What a pin is called, and how it is recognised as ours after a reload.
    ///
    /// A saved pin keeps its name, position, type and checked state, and no sprite, so on the way
    /// back in the icon has to be worked out again. The name is the only thing that can carry that,
    /// which is safe here because Valheim has no way to rename a pin: placing one asks for a name
    /// and makes a new pin, and clicking an existing one only ticks it off.
    ///
    /// Names are taken from the game rather than written out here. A pickable knows the item it
    /// gives, a deposit knows what it drops, and those are localisation tokens, so the save holds a
    /// token and the player reads it in their own language.
    /// </summary>
    public static class PinNames
    {
        private static Dictionary<string, PinCategory> _categories;

        /// <summary>
        /// What a pin placed from this rule is called.
        ///
        /// The curated name wins when there is one. Some objects carry nothing but a drop table: the
        /// wild beehive is not the buildable piece and has no name of its own, so read off the object
        /// it would come out as Honey.
        /// </summary>
        public static string For(PinRules.Rule rule, GameObject prefab)
        {
            if (!string.IsNullOrEmpty(rule.NameToken)) return rule.NameToken;

            return For(prefab) ?? rule.Name;
        }

        /// <summary>
        /// The name for a prefab, or null when nothing sensible can be read off it.
        /// </summary>
        public static string For(GameObject prefab)
        {
            if (prefab == null) return null;

            // The game's own name for the thing, when it has one. A beehive is a beehive; naming it
            // after what falls out of it reads as though the honey were lying on the ground.
            var piece = prefab.GetComponent<Piece>();
            if (piece != null && !string.IsNullOrEmpty(piece.m_name)) return piece.m_name;

            // Otherwise it is named for what it gives, which is why anyone is walking towards it.
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

            // World generation places the intact rock, which carries no harvest component at all:
            // the MineRock lives on the fractured object it swaps to when hit.
            var destructible = prefab.GetComponent<Destructible>();
            if (destructible?.m_spawnWhenDestroyed != null) return For(destructible.m_spawnWhenDestroyed);

            var hover = prefab.GetComponent<HoverText>();
            if (hover != null && !string.IsNullOrEmpty(hover.m_text)) return hover.m_text;

            return null;
        }

        /// <summary>
        /// Which category a pin belongs to, worked out from its name. Built from the same rules and
        /// the same prefabs the pins were placed from, so the two always agree.
        /// </summary>
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
                    var prefab = scene.GetPrefab(prefabName);
                    if (prefab == null) continue;

                    var name = For(rule, prefab);

                    // Two rules can land on one name, the same berry in two biomes. They agree on
                    // the category, which is all this is asked for.
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

        /// <summary>
        /// A deposit drops the rock it was buried in as well as the metal, and the rock comes first:
        /// taking the first drop names every copper deposit "Stone". The filler is skipped so the
        /// name lands on what anyone came for.
        /// </summary>
        private static readonly HashSet<string> Filler = new HashSet<string>
        {
            "Stone", "Wood", "RoundLog", "FineWood", "ElderBark", "YggdrasilWood", "Grausten"
        };

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

            // Everything it drops is filler, so that is what it is.
            return filler;
        }
    }
}
