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
        private static List<PinRules.Rule> _byComponent;
        private static List<System.Type> _componentTypes;

        // Which kinds of location have ever been seen to hold one of the things we look for inside
        // them, so the other twenty odd kinds are searched once and skipped forever after. Only ever
        // told about a location that has finished building: a proxy still waiting on its contents has
        // nothing to say yet, and writing down "nothing here" would make that permanent.
        private static readonly Dictionary<int, bool> _holds = new Dictionary<int, bool>();

        private static readonly AccessTools.FieldRef<LocationProxy, GameObject> ProxyInstance =
            AccessTools.FieldRefAccess<LocationProxy, GameObject>("m_instance");


        private static readonly int VanillaTypes = System.Enum.GetValues(typeof(Minimap.PinType)).Length;

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
            if (_byPrefab.Count == 0 && _byLocation.Count == 0 && _byComponent.Count == 0) return;

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
                    var location = zdo.GetInt(ZDOVars.s_location, 0);

                    Inside(location, pair.Value, zdo.GetPosition(), origin, rangeSqr, merge);

                    if (!_byLocation.TryGetValue(location, out watch)) continue;
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

        // Vegvisirs and their like are built into a location rather than spawned as objects of their
        // own, so they have no ZDO and this sweep cannot see them. What it can see is the proxy the
        // game leaves standing, and the location is parented to it, so they are reachable from there.
        //
        // Each is pinned where it stands and judged on the biome it stands in, not on the location's,
        // because a ruin can straddle a border. A dungeon's interior is built far from its entrance
        // and so falls outside the range check on its own, which is how it stays unpinned.
        public static void Inside(int location, ZNetView nview, Vector3 where, Vector3 origin, float rangeSqr,
            float merge)
        {
            if (_byComponent == null || _byComponent.Count == 0) return;
            if (nview == null || (where - origin).sqrMagnitude > rangeSqr) return;

            if (_holds.TryGetValue(location, out var holds) && !holds) return;

            var proxy = nview.GetComponent<LocationProxy>();
            if (proxy == null) return;

            // Nothing is concluded about a location that has not finished building. Writing down
            // "holds none" here would be permanent, and the contents arrive a moment later.
            if (ProxyInstance != null && ProxyInstance(proxy) == null) return;

            var any = false;

            // Once per kind of thing, not once per rule. The rules are the same component seven times
            // over, one per biome, and each scan walks the whole location.
            foreach (var type in _componentTypes)
            {
                var found = nview.GetComponentsInChildren(type, true);
                if (found.Length == 0) continue;

                any = true;

                foreach (var component in found)
                {
                    if (component == null) continue;

                    var position = component.transform.position;
                    if ((position - origin).sqrMagnitude > rangeSqr) continue;

                    var biome = Heightmap.FindBiome(position);

                    foreach (var rule in _byComponent)
                    {
                        if (rule.Component != type || rule.Biome != biome) continue;
                        if (rule.Enabled == null || !rule.Enabled.Value) break;

                        if (PinRecord.Has(rule.Category, position, merge)) break;

                        Place(rule, null, position);
                        break;
                    }
                }
            }

            _holds[location] = any;
        }

        public static void Spawned(LocationProxy proxy)
        {
            if (!Plugin.autoPin.Value || _byComponent == null || _byComponent.Count == 0) return;
            if (proxy == null || Player.m_localPlayer == null || Minimap.instance == null) return;

            var nview = proxy.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;

            var range = Plugin.explorationRadius.Value;

            // No flush here. Locations spawn in their hundreds as the world loads, and the record is
            // written out by the sweep a moment later anyway.
            Inside(nview.GetZDO().GetInt(ZDOVars.s_location, 0), nview, proxy.transform.position,
                Player.m_localPlayer.transform.position, range * range, Plugin.autoPinMergeDistance.Value);
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
                    // The number a category ends up with depends on what else claimed a slot before
                    // us, so a pin saved under one set of mods can come back under another and no
                    // longer match. Its name still does. Only pins that could be ours are asked:
                    // a portal tagged "Troll Caves" is not a troll cave.
                    if ((int)pin.m_type < VanillaTypes && pin.m_type != Plugin.AutoPinType) continue;
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
            _byComponent = new List<PinRules.Rule>();
            _componentTypes = new List<System.Type>();

            foreach (var rule in PinRules.All)
            {
                if (rule.Component != null)
                {
                    _byComponent.Add(rule);

                    if (!_componentTypes.Contains(rule.Component)) _componentTypes.Add(rule.Component);

                    continue;
                }

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
                Plugin.Logger.LogInfo($"AutoPins: watching {_byPrefab.Count} prefabs, {_byLocation.Count} places and {_byComponent.Count} built in");
            }
        }

        private static void Verify()
        {
            var scene = ZNetScene.instance;
            var zones = ZoneSystem.instance;

            foreach (var rule in PinRules.All)
            {
                if (rule.Component != null) continue;

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
