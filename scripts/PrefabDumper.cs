using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts
{
    /// <summary>
    /// One-off diagnostic. Walks every networked prefab in the installed game and writes down what
    /// is harvestable, what each thing drops, which creatures have a trophy, and which prefabs are
    /// locations or vehicles.
    ///
    /// This exists so the pin rules can be written against the game that is actually installed,
    /// including anything a Valheim update or another mod added, instead of against a wiki. It is
    /// not a feature and should be deleted once the rules are settled.
    /// </summary>
    [HarmonyPatch]
    public static class PrefabDumper
    {
        private static bool _dumped;

        [HarmonyPostfix, HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static void ZNetScene_Awake_Postfix(ZNetScene __instance)
        {
            if (_dumped || !Plugin.dumpPrefabs.Value) return;
            _dumped = true;

            try
            {
                Dump(__instance);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"PrefabDumper failed: {e}");
            }
        }

        private static void Dump(ZNetScene scene)
        {
            var harvestables = new List<string>();
            var creaturesWithTrophy = new List<string>();
            var creaturesWithoutTrophy = new List<string>();
            var locations = new List<string>();
            var vehicles = new List<string>();
            var containers = new List<string>();

            foreach (var prefab in scene.m_prefabs)
            {
                if (prefab == null) continue;

                var name = prefab.name;

                // Harvestables, by component rather than by name.
                var pickable = prefab.GetComponent<Pickable>();
                if (pickable != null)
                {
                    harvestables.Add($"{name,-40} Pickable          {Describe(pickable.m_itemPrefab, pickable.m_amount)}");
                }

                var pickableItem = prefab.GetComponent<PickableItem>();
                if (pickableItem != null)
                {
                    harvestables.Add($"{name,-40} PickableItem      {DescribeItemDrop(pickableItem.m_itemPrefab)}");
                }

                var mineRock = prefab.GetComponent<MineRock>();
                if (mineRock != null)
                {
                    harvestables.Add($"{name,-40} MineRock          {Describe(mineRock.m_dropItems)}");
                }

                var mineRock5 = prefab.GetComponent<MineRock5>();
                if (mineRock5 != null)
                {
                    harvestables.Add($"{name,-40} MineRock5         {Describe(mineRock5.m_dropItems)}");
                }

                if (prefab.GetComponent<ResourceRoot>() != null)
                {
                    harvestables.Add($"{name,-40} ResourceRoot");
                }

                if (prefab.GetComponent<Beehive>() != null)
                {
                    harvestables.Add($"{name,-40} Beehive");
                }

                var dropOnDestroyed = prefab.GetComponent<DropOnDestroyed>();
                if (dropOnDestroyed != null && dropOnDestroyed.m_dropWhenDestroyed?.m_drops?.Count > 0)
                {
                    harvestables.Add($"{name,-40} DropOnDestroyed   {Describe(dropOnDestroyed.m_dropWhenDestroyed)}");
                }

                // Creatures, and whether a trophy icon can be derived for them.
                if (prefab.GetComponent<Character>() != null)
                {
                    var trophy = FindTrophy(prefab);
                    var tameable = prefab.GetComponent<Tameable>() != null ? "  [tameable]" : "";

                    if (trophy != null)
                        creaturesWithTrophy.Add($"{name,-40} {trophy}{tameable}");
                    else
                        creaturesWithoutTrophy.Add($"{name,-40} no trophy{tameable}");
                }

                if (prefab.GetComponent<Location>() != null) locations.Add(name);
                if (prefab.GetComponent<Ship>() != null) vehicles.Add($"{name,-40} Ship");
                if (prefab.GetComponent<Vagon>() != null) vehicles.Add($"{name,-40} Vagon");
                if (prefab.GetComponent<Container>() != null) containers.Add(name);
            }

            var report = new StringBuilder();
            report.AppendLine($"BetterMap prefab dump");
            report.AppendLine($"Valheim {Version.GetVersionString()}   {scene.m_prefabs.Count} prefabs");
            report.AppendLine();

            Section(report, "HARVESTABLES", harvestables);
            Section(report, "CREATURES WITH A TROPHY", creaturesWithTrophy);
            Section(report, "CREATURES WITHOUT A TROPHY (need the name fallback)", creaturesWithoutTrophy);
            Section(report, "VEHICLES", vehicles);
            Section(report, "LOCATIONS", locations);
            Section(report, "CONTAINERS", containers);

            var path = Path.Combine(Paths.ConfigPath, "BetterMap.prefabs.txt");
            File.WriteAllText(path, report.ToString());

            Plugin.Logger.LogInfo($"PrefabDumper: wrote {path}");
            Plugin.Logger.LogInfo($"PrefabDumper: {harvestables.Count} harvestables, " +
                                  $"{creaturesWithTrophy.Count} creatures with a trophy, " +
                                  $"{creaturesWithoutTrophy.Count} without, " +
                                  $"{locations.Count} locations, {vehicles.Count} vehicles");
        }

        private static void Section(StringBuilder report, string title, List<string> lines)
        {
            report.AppendLine(new string('=', 100));
            report.AppendLine($"{title}  ({lines.Count})");
            report.AppendLine(new string('=', 100));

            foreach (var line in lines.OrderBy(l => l, StringComparer.OrdinalIgnoreCase))
            {
                report.AppendLine(line);
            }

            report.AppendLine();
        }

        // The same derivation the creature pins will use, so the dump shows exactly which creatures
        // that lookup succeeds for.
        private static string FindTrophy(GameObject prefab)
        {
            var drops = prefab.GetComponent<CharacterDrop>();
            if (drops?.m_drops == null) return null;

            foreach (var drop in drops.m_drops)
            {
                if (drop?.m_prefab == null) continue;
                if (!drop.m_prefab.name.StartsWith("Trophy", StringComparison.OrdinalIgnoreCase)) continue;

                var item = drop.m_prefab.GetComponent<ItemDrop>();
                var hasIcon = item?.m_itemData?.m_shared?.m_icons?.Length > 0;

                return hasIcon ? drop.m_prefab.name : drop.m_prefab.name + " (NO ICON)";
            }

            return null;
        }

        private static string Describe(GameObject item, int amount)
        {
            return item == null ? "(nothing)" : $"{item.name} x{amount}";
        }

        private static string DescribeItemDrop(ItemDrop item)
        {
            return item == null ? "(nothing)" : item.name;
        }

        private static string Describe(DropTable table)
        {
            if (table?.m_drops == null || table.m_drops.Count == 0) return "(nothing)";

            var parts = table.m_drops
                .Where(d => d.m_item != null)
                .Select(d => $"{d.m_item.name} x{d.m_stackMin}-{d.m_stackMax}");

            return string.Join(", ", parts);
        }
    }
}
