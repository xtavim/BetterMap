using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Pins
{
    /// <summary>
    /// Pins resources as you come across them.
    ///
    /// Detection is a sweep of what the game currently has loaded, on an interval. Objects are read
    /// through their ZDOs rather than their GameObjects, which is enough for a prefab and a
    /// position and touches no components.
    ///
    /// Only objects scattered by world generation are handled here. Places, the crypts and caves and
    /// fortresses, are put together differently and are not detected this way.
    /// </summary>
    public static class AutoPins
    {
        private static readonly AccessTools.FieldRef<ZNetScene, Dictionary<ZDO, ZNetView>> Instances =
            AccessTools.FieldRefAccess<ZNetScene, Dictionary<ZDO, ZNetView>>("m_instances");

        private class Watch
        {
            // Several rules can share a prefab, the same berry bush in two biomes with two answers.
            public readonly List<PinRules.Rule> Rules = new List<PinRules.Rule>();

            // Set when every rule for this prefab agrees on a category, which is the usual case and
            // is what lets the record be consulted before the biome.
            public PinCategory? Shared;
        }

        private static Dictionary<int, Watch> _byPrefab;

        private static float _nextSweep;
        private static bool _forgotten;

        public static void Tick()
        {
            if (!Plugin.autoPin.Value) return;
            if (Minimap.instance == null || Player.m_localPlayer == null || ZNetScene.instance == null) return;

            if (Time.time < _nextSweep) return;
            _nextSweep = Time.time + Plugin.autoPinInterval.Value;

            Build();
            if (_byPrefab.Count == 0) return;

            if (Plugin.forgetPinned.Value && !_forgotten)
            {
                _forgotten = true;
                PinRecord.Wipe();
            }

            Sweep();
            PinRecord.Flush();
        }

        private static void Sweep()
        {
            var origin = Player.m_localPlayer.transform.position;
            var range = Plugin.explorationRadius.Value;
            var rangeSqr = range * range;
            var merge = Plugin.autoPinMergeDistance.Value;

            foreach (var pair in Instances(ZNetScene.instance))
            {
                var zdo = pair.Key;
                if (zdo == null || !zdo.IsValid()) continue;

                if (!_byPrefab.TryGetValue(zdo.GetPrefab(), out var watch)) continue;

                var position = zdo.GetPosition();
                if ((position - origin).sqrMagnitude > rangeSqr) continue;

                // Nearly everything around you has been pinned already, and the two questions cost
                // very different amounts: the record is a lookup in a grid, while finding a biome
                // walks every loaded heightmap looking for the one holding the point. Ask the cheap
                // one first whenever the answer cannot depend on the biome.
                if (watch.Shared.HasValue && PinRecord.Has(watch.Shared.Value, position, merge)) continue;

                // The same thing can be worth pinning in one biome and not in another, so the answer
                // depends on where it stands rather than on what it is.
                var biome = Heightmap.FindBiome(position);

                foreach (var rule in watch.Rules)
                {
                    if (rule.Biome != biome) continue;
                    if (rule.Enabled == null || !rule.Enabled.Value) continue;

                    if (PinRecord.Has(rule.Category, position, merge)) break;

                    Place(rule, pair.Value, position);
                    break;
                }
            }
        }

        private static void Place(PinRules.Rule rule, ZNetView nview, Vector3 position)
        {
            var name = PinNames.For(rule, nview != null ? nview.gameObject : null);

            var pin = Minimap.instance.AddPin(position, Plugin.AutoPinType, name,
                save: true, isChecked: false);

            var icon = Icons.For(rule.Category);
            if (icon != null) pin.m_icon = icon;

            if (!string.IsNullOrEmpty(pin.m_name)) pin.m_NamePinData = new Minimap.PinNameData(pin);

            PinRecord.Add(rule.Category, position);

            if (Plugin.debugMode.Value)
            {
                Plugin.Logger.LogInfo($"AutoPins: {rule.Name} ({rule.Category}) at {position} as \"{pin.m_name}\"");
            }
        }

        /// <summary>
        /// Puts the icons back on pins the save brought in. A saved pin keeps no sprite, so without
        /// this every resource on a reloaded map comes back wearing the same generic marker.
        /// </summary>
        public static void Restore()
        {
            if (Minimap.instance == null || ZNetScene.instance == null) return;

            PinNames.Build();

            var restored = 0;

            foreach (var pin in MapPins.Of(Minimap.instance))
            {
                if (pin.m_type != Plugin.AutoPinType) continue;
                if (!PinNames.Category(pin.m_name, out var category)) continue;

                var icon = Icons.For(category);
                if (icon == null || pin.m_icon == icon) continue;

                pin.m_icon = icon;
                restored++;
            }

            if (restored > 0 && Plugin.debugMode.Value) Plugin.Logger.LogInfo($"AutoPins: {restored} icons put back");
        }

        private static void Build()
        {
            if (_byPrefab != null) return;

            _byPrefab = new Dictionary<int, Watch>();

            foreach (var rule in PinRules.All)
            {
                // Places are not found by sweeping loaded objects.
                if (rule.IsLocation) continue;

                foreach (var prefabName in rule.Prefabs)
                {
                    var hash = prefabName.GetStableHashCode();

                    if (!_byPrefab.TryGetValue(hash, out var watch))
                    {
                        watch = new Watch();
                        _byPrefab[hash] = watch;
                    }

                    watch.Rules.Add(rule);
                }
            }

            foreach (var watch in _byPrefab.Values)
            {
                watch.Shared = watch.Rules[0].Category;

                foreach (var rule in watch.Rules)
                {
                    if (rule.Category != watch.Shared) watch.Shared = null;
                }
            }

            if (Plugin.debugMode.Value) Plugin.Logger.LogInfo($"AutoPins: watching {_byPrefab.Count} prefabs");
        }
    }
}
