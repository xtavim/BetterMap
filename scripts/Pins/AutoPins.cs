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
    /// Places are found the same way. A crypt is not a prefab you can look for, but the game leaves a
    /// LocationProxy standing where it put one, carrying the location's name on its ZDO, and that
    /// proxy is spawned when the zone loads. So the same sweep finds both, and a place is discovered
    /// by being near it rather than by asking the server what the world contains, which would hand
    /// over every crypt on the map at once.
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

            // A place has no prefab of its own to read a name off, so it is named by the curated list.
            public bool IsLocation;
        }

        private static Dictionary<int, Watch> _byPrefab;
        private static Dictionary<int, Watch> _byLocation;

        private static int _proxyHash;

        private static float _nextSweep;
        private static bool _forgotten;

        public static void Tick()
        {
            if (!Plugin.autoPin.Value) return;
            if (Minimap.instance == null || Player.m_localPlayer == null || ZNetScene.instance == null) return;

            if (Time.time < _nextSweep) return;
            _nextSweep = Time.time + Plugin.autoPinInterval.Value;

            Build();
            if (_byPrefab.Count == 0 && _byLocation.Count == 0) return;

            if (_proxyHash == 0 && ZoneSystem.instance != null && ZoneSystem.instance.m_locationProxyPrefab != null)
            {
                _proxyHash = ZoneSystem.instance.m_locationProxyPrefab.name.GetStableHashCode();
            }

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

                var prefab = zdo.GetPrefab();

                if (PortalPins.IsPortal(prefab))
                {
                    Portal(zdo, origin, rangeSqr, merge);
                    continue;
                }

                Watch watch;

                if (prefab == _proxyHash)
                {
                    // A place, named on the proxy the game leaves where it put one.
                    if (!_byLocation.TryGetValue(zdo.GetInt(ZDOVars.s_location, 0), out watch)) continue;
                }
                else if (!_byPrefab.TryGetValue(prefab, out watch))
                {
                    continue;
                }

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

                    Place(rule, watch.IsLocation ? null : pair.Value, position);
                    break;
                }
            }
        }

        /// <summary>
        /// A portal is not on the curated list: it is something the player built, and it is pinned
        /// wherever it stands rather than because of the biome it stands in.
        /// </summary>
        private static void Portal(ZDO zdo, Vector3 origin, float rangeSqr, float merge)
        {
            if (!Plugin.autoPinPortals.Value) return;

            var position = zdo.GetPosition();
            if ((position - origin).sqrMagnitude > rangeSqr) return;

            if (PinRecord.Has(PinCategory.Portal, position, merge)) return;

            var pin = Minimap.instance.AddPin(position, PortalPins.PinType,
                PortalPins.Label(PortalPins.TagOf(zdo)), save: true, isChecked: false);

            pin.m_NamePinData = new Minimap.PinNameData(pin);

            PinRecord.Add(PinCategory.Portal, position);

            if (Plugin.debugMode.Value) Plugin.Logger.LogInfo("AutoPins: portal at " + position + " as " + pin.m_name);
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
            _byLocation = new Dictionary<int, Watch>();

            foreach (var rule in PinRules.All)
            {
                var into = rule.IsLocation ? _byLocation : _byPrefab;

                foreach (var prefabName in rule.Prefabs)
                {
                    var hash = prefabName.GetStableHashCode();

                    if (!into.TryGetValue(hash, out var watch))
                    {
                        watch = new Watch { IsLocation = rule.IsLocation };
                        into[hash] = watch;
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

            foreach (var watch in _byLocation.Values)
            {
                watch.Shared = watch.Rules[0].Category;

                foreach (var rule in watch.Rules)
                {
                    if (rule.Category != watch.Shared) watch.Shared = null;
                }
            }

            Verify();

            if (Plugin.debugMode.Value)
            {
                Plugin.Logger.LogInfo($"AutoPins: watching {_byPrefab.Count} prefabs and {_byLocation.Count} places");
            }
        }

        /// <summary>
        /// Says so when a rule can never match anything.
        ///
        /// A rule names a prefab or a place, and a name that is neither simply never comes up in the
        /// sweep: the setting is there, the box is ticked, and nothing is ever pinned. That happened
        /// to the tar pits, which are places and were written down as objects, and there was nothing
        /// to see. Checking the names against the game turns a silent nothing into a line in the log.
        /// </summary>
        private static void Verify()
        {
            var scene = ZNetScene.instance;
            var zones = ZoneSystem.instance;

            foreach (var rule in PinRules.All)
            {
                foreach (var name in rule.Prefabs)
                {
                    if (rule.IsLocation)
                    {
                        if (zones == null || zones.m_locations == null) continue;

                        var known = false;

                        foreach (var location in zones.m_locations)
                        {
                            if (location != null && location.m_prefabName == name)
                            {
                                known = true;
                                break;
                            }
                        }

                        if (!known)
                        {
                            Plugin.Logger.LogWarning(
                                $"AutoPins: \"{rule.Name}\" watches a place named {name}, which this game has none of. Nothing will ever be pinned for it.");
                        }

                        continue;
                    }

                    if (scene != null && scene.GetPrefab(name) == null)
                    {
                        Plugin.Logger.LogWarning(
                            $"AutoPins: \"{rule.Name}\" watches a prefab named {name}, which this game has none of. Nothing will ever be pinned for it.");
                    }
                }
            }
        }
    }
}
