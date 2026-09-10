using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Pins
{
    public static class AutoPins
    {
        private static readonly AccessTools.FieldRef<ZNetScene, Dictionary<ZDO, ZNetView>> Instances =
            AccessTools.FieldRefAccess<ZNetScene, Dictionary<ZDO, ZNetView>>("m_instances");

        private class Watch
        {
            public readonly List<PinRules.Rule> Rules = new List<PinRules.Rule>();

            public PinCategory? Shared;

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

        // A place is not a prefab to look for, but the game leaves a LocationProxy standing where
        // it put one, carrying the location's name. So one sweep finds objects and places both.
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
                    if (!_byLocation.TryGetValue(zdo.GetInt(ZDOVars.s_location, 0), out watch)) continue;
                }
                else if (!_byPrefab.TryGetValue(prefab, out watch))
                {
                    continue;
                }

                var position = zdo.GetPosition();
                if ((position - origin).sqrMagnitude > rangeSqr) continue;

                if (watch.Shared.HasValue && PinRecord.Has(watch.Shared.Value, position, merge)) continue;

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

            var pin = Minimap.instance.AddPin(position, PinLegend.TypeOf(rule.Category), name,
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

        public static void Restore()
        {
            if (Minimap.instance == null || ZNetScene.instance == null) return;

            PinNames.Build();

            var restored = 0;

            foreach (var pin in MapPins.Of(Minimap.instance))
            {
                if (!PinLegend.CategoryOf(pin.m_type, out var category))
                {
                    if (pin.m_type != Plugin.AutoPinType) continue;
                    if (!PinNames.Category(pin.m_name, out category)) continue;
                }

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
