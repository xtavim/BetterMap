using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using SoftReferenceableAssets;

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
    public static class VegetationDumper
    {
        private static bool _dumped;

        /// <summary>
        /// Polled rather than patched onto a lifecycle method. m_vegetation is not the serialized
        /// list: ZoneSystem.Start calls SetupLocations, which merges in the per biome location
        /// lists, and only then is Mistlands, Ashlands and Deep North content present. Waiting for
        /// the player to spawn puts us safely after all of it.
        /// </summary>
        public static void TryDump()
        {
            if (_dumped || !Plugin.dumpPrefabs.Value) return;

            var zones = ZoneSystem.instance;
            if (zones == null || Player.m_localPlayer == null) return;
            if (zones.m_vegetation == null || zones.m_vegetation.Count == 0) return;

            _dumped = true;

            try
            {
                Dump(zones);
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

            DumpLocations(zones);
        }

        /// <summary>
        /// Locations are the other half of the picture. Some resource sites are not scattered as
        /// vegetation but placed inside a location: flametal and gold never appear in the
        /// vegetation dump for that reason.
        ///
        /// Only metadata is read here. The prefab behind a location is a soft reference and would
        /// have to be loaded to see what is inside it, which is not worth doing for a few hundred
        /// locations until the names turn out to be ambiguous.
        /// </summary>
        private static void DumpLocations(ZoneSystem zones)
        {
            var byBiome = new Dictionary<string, List<string>>();

            foreach (var loc in zones.m_locations)
            {
                if (loc == null || !loc.m_enable) continue;

                var icon = loc.m_iconAlways ? "icon:always" : loc.m_iconPlaced ? "icon:placed" : "";
                var unique = loc.m_unique ? "unique" : "";

                var line = $"  {loc.m_prefabName,-42} qty {loc.m_quantity,-6} {icon,-12} {unique}".TrimEnd();

                foreach (var content in Contents(loc))
                {
                    line += Environment.NewLine + "        " + content;
                }

                foreach (var biome in Biomes(loc.m_biome))
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
            report.AppendLine("BetterMap location dump");
            report.AppendLine($"Valheim {Version.GetVersionString()}   {zones.m_locations.Count} locations");
            report.AppendLine();
            report.AppendLine("Resource sites placed inside a location rather than scattered as vegetation.");
            report.AppendLine("icon:always and icon:placed are locations vanilla already puts on the map itself.");
            report.AppendLine();

            foreach (var biome in byBiome.Keys.OrderBy(b => b, StringComparer.OrdinalIgnoreCase))
            {
                report.AppendLine(new string('=', 100));
                report.AppendLine($"{biome}  ({byBiome[biome].Count})");
                report.AppendLine(new string('=', 100));

                foreach (var line in byBiome[biome].OrderBy(l => l, StringComparer.OrdinalIgnoreCase))
                {
                    report.AppendLine(line);
                }

                report.AppendLine();
            }

            var path = Path.Combine(Paths.ConfigPath, "BetterMap.locations.txt");
            File.WriteAllText(path, report.ToString());

            Plugin.Logger.LogInfo($"VegetationDumper: wrote {path} ({zones.m_locations.Count} locations)");
        }

        /// <summary>
        /// What is actually inside a location. Beehives live in Meadows houses, flametal inside
        /// LeviathanLava, gold inside the frozen trolls: none of that is scattered as vegetation,
        /// so a location that is only described by its name hides everything worth pinning in it.
        ///
        /// The prefab is a soft reference, so it is loaded, walked and released again. Slow, but
        /// this runs once and only when the dump is switched on.
        /// </summary>
        private static IEnumerable<string> Contents(ZoneSystem.ZoneLocation loc)
        {
            GameObject asset;

            try
            {
                if (loc.m_prefab.Load() != LoadResult.Succeeded || !loc.m_prefab.IsLoaded)
                {
                    yield break;
                }

                asset = loc.m_prefab.Asset;
            }
            finally
            {
            }

            var harvest = new Dictionary<string, int>();
            var spawners = new Dictionary<string, int>();

            foreach (var t in asset.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;
                var name = CleanName(go.name);

                var drops = DescribeDirectHarvest(go);
                if (drops != null)
                {
                    var key = $"{name,-38} {drops}";
                    harvest[key] = harvest.TryGetValue(key, out var n) ? n + 1 : 1;
                }

                if (go.GetComponent<SpawnArea>() != null) Count(spawners, name + "  [SpawnArea]");
                if (go.GetComponent<CreatureSpawner>() != null) Count(spawners, name + "  [CreatureSpawner]");
                if (go.GetComponent<Container>() != null) Count(spawners, name + "  [Container]");
                if (go.GetComponent<Vegvisir>() != null) Count(spawners, name + "  [Vegvisir]");
                if (go.GetComponent<RuneStone>() != null) Count(spawners, name + "  [RuneStone]");
                if (go.GetComponent<OfferingBowl>() != null) Count(spawners, name + "  [OfferingBowl]");
            }

            loc.m_prefab.Release();

            foreach (var kv in harvest.OrderByDescending(k => k.Value))
            {
                yield return $"x{kv.Value,-4} {kv.Key}";
            }

            foreach (var kv in spawners.OrderByDescending(k => k.Value))
            {
                yield return $"x{kv.Value,-4} {kv.Key}";
            }
        }

        private static void Count(Dictionary<string, int> into, string key)
        {
            into[key] = into.TryGetValue(key, out var n) ? n + 1 : 1;
        }

        private static string CleanName(string name)
        {
            var i = name.IndexOf("(Clone)", StringComparison.Ordinal);
            return i < 0 ? name : name.Substring(0, i);
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
        //
        // World generation places the intact rock, which carries no harvest component at all: the
        // MineRock lives on the fractured object it swaps to when hit. Copper and silver both look
        // like scenery unless that reference is followed.
        private static string DescribeHarvest(GameObject prefab)
        {
            var direct = DescribeDirectHarvest(prefab);
            if (direct != null) return direct;

            var destructible = prefab.GetComponent<Destructible>();
            var fractured = destructible?.m_spawnWhenDestroyed;
            if (fractured == null) return null;

            var indirect = DescribeDirectHarvest(fractured);
            return indirect == null ? null : $"{indirect}   [via {fractured.name}]";
        }

        private static string DescribeDirectHarvest(GameObject prefab)
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
