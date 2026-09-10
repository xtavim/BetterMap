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
    /// One-off diagnostic, companion to PrefabDumper.
    ///
    /// Dumps everything world generation scatters across the map, grouped by biome, with what each
    /// thing yields when harvested. Dungeon interiors are not in here: those props are placed inside
    /// location prefabs, which is exactly why they are not pin candidates.
    ///
    /// This is the menu the pin whitelist is curated from, not a rule in itself.
    /// </summary>
    [HarmonyPatch]
    public static class VegetationDumper
    {
        private static bool _dumped;

        [HarmonyPostfix, HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Awake))]
        private static void ZoneSystem_Awake_Postfix(ZoneSystem __instance)
        {
            if (_dumped || !Plugin.dumpPrefabs.Value) return;
            _dumped = true;

            try
            {
                Dump(__instance);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"VegetationDumper failed: {e}");
            }
        }

        private static void Dump(ZoneSystem zones)
        {
            // A vegetation entry can be flagged for several biomes at once, so it is listed under
            // each one it can appear in.
            var byBiome = new Dictionary<string, List<string>>();
            int withDrops = 0;

            foreach (var veg in zones.m_vegetation)
            {
                if (veg?.m_prefab == null || !veg.m_enable) continue;

                var drops = DescribeHarvest(veg.m_prefab);
                if (drops != null) withDrops++;

                var density = veg.m_max <= 1f
                    ? $"{veg.m_min:0.##}-{veg.m_max:0.##}"
                    : $"{veg.m_min:0}-{veg.m_max:0}";

                var line = $"  {veg.m_prefab.name,-42} qty {density,-12} {drops ?? ""}".TrimEnd();

                foreach (var biome in Biomes(veg.m_biome))
                {
                    if (!byBiome.TryGetValue(biome, out var list))
                    {
                        list = new List<string>();
                        byBiome[biome] = list;
                    }

                    list.Add(line);
                }
            }

            var report = new StringBuilder();
            report.AppendLine("BetterMap vegetation dump");
            report.AppendLine($"Valheim {Version.GetVersionString()}   {zones.m_vegetation.Count} vegetation entries, {withDrops} of them harvestable");
            report.AppendLine();
            report.AppendLine("Everything world generation scatters, by biome. Dungeon interiors are absent by design.");
            report.AppendLine();

            foreach (var biome in byBiome.Keys.OrderBy(b => b, StringComparer.OrdinalIgnoreCase))
            {
                var lines = byBiome[biome];

                report.AppendLine(new string('=', 100));
                report.AppendLine($"{biome}  ({lines.Count})");
                report.AppendLine(new string('=', 100));

                // Harvestable things first: those are the pin candidates.
                foreach (var line in lines.Where(l => l.Contains("->")).OrderBy(l => l, StringComparer.OrdinalIgnoreCase))
                {
                    report.AppendLine(line);
                }

                report.AppendLine();

                foreach (var line in lines.Where(l => !l.Contains("->")).OrderBy(l => l, StringComparer.OrdinalIgnoreCase))
                {
                    report.AppendLine(line);
                }

                report.AppendLine();
            }

            var path = Path.Combine(Paths.ConfigPath, "BetterMap.vegetation.txt");
            File.WriteAllText(path, report.ToString());

            Plugin.Logger.LogInfo($"VegetationDumper: wrote {path} ({byBiome.Count} biomes, {withDrops} harvestable)");
        }

        private static IEnumerable<string> Biomes(Heightmap.Biome biome)
        {
            var any = false;

            foreach (Heightmap.Biome value in Enum.GetValues(typeof(Heightmap.Biome)))
            {
                if (value == Heightmap.Biome.None) continue;
                if ((biome & value) != value) continue;

                any = true;
                yield return value.ToString();
            }

            if (!any) yield return "None";
        }

        // Only the components that actually yield something. Anything with no harvest is scenery.
        private static string DescribeHarvest(GameObject prefab)
        {
            var pickable = prefab.GetComponent<Pickable>();
            if (pickable?.m_itemPrefab != null)
                return $"Pickable   -> {pickable.m_itemPrefab.name} x{pickable.m_amount}";

            var mineRock = prefab.GetComponent<MineRock>();
            if (mineRock != null)
                return $"MineRock   -> {Describe(mineRock.m_dropItems)}  (tier {mineRock.m_minToolTier}, hp {mineRock.m_health:0})";

            var mineRock5 = prefab.GetComponent<MineRock5>();
            if (mineRock5 != null)
                return $"MineRock5  -> {Describe(mineRock5.m_dropItems)}  (tier {mineRock5.m_minToolTier}, hp {mineRock5.m_health:0})";

            if (prefab.GetComponent<ResourceRoot>() != null)
                return "ResourceRoot -> sap";

            if (prefab.GetComponent<Beehive>() != null)
                return "Beehive    -> honey";

            var destroyed = prefab.GetComponent<DropOnDestroyed>();
            if (destroyed?.m_dropWhenDestroyed?.m_drops?.Count > 0)
                return $"Destroyed  -> {Describe(destroyed.m_dropWhenDestroyed)}";

            return null;
        }

        private static string Describe(DropTable table)
        {
            if (table?.m_drops == null || table.m_drops.Count == 0) return "(nothing)";

            return string.Join(", ", table.m_drops
                .Where(d => d.m_item != null)
                .Select(d => $"{d.m_item.name} x{d.m_stackMin}-{d.m_stackMax}"));
        }
    }
}
