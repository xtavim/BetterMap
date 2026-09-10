using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Vehicles
{
    /// <summary>
    /// Boats and carts on the map, from two places at once.
    ///
    /// Near the player, whoever owns them: taken from the loaded Ship and Vagon instances, which are
    /// the real objects, so their pins follow them frame by frame while someone is driving.
    ///
    /// Anywhere in the world, but only yours: what you built and what you have driven, answered by
    /// the server. Those do not need to be smooth, because a vehicle nobody has loaded is a vehicle
    /// nobody is moving.
    ///
    /// The split is a decision, not a limitation. A stranger's boat appearing across the map would
    /// be one you had no way of knowing about, so it is held to the same rule as a creature: close
    /// enough to see.
    /// </summary>
    public static class VehicleTracker
    {
        private class Tracked
        {
            public Minimap.PinData Pin;
            public Transform Instance;
            public float Heading;
        }

        private struct Vehicle
        {
            public Transform Instance;
            public Vector3 Pos;
            public float Heading;
            public VehicleKinds.Kind Kind;
        }

        private static readonly AccessTools.FieldRef<List<Vagon>> Carts =
            AccessTools.StaticFieldRefAccess<List<Vagon>>(AccessTools.Field(typeof(Vagon), "m_instances"));

        private static readonly Dictionary<ZDOID, Tracked> _tracked = new Dictionary<ZDOID, Tracked>();
        private static readonly Dictionary<ZDOID, Vehicle> _wanted = new Dictionary<ZDOID, Vehicle>();
        private static readonly List<ZDOID> _gone = new List<ZDOID>();

        private static bool _named;

        public static void Tick()
        {
            var wantBoats = Plugin.showBoats.Value;
            var wantCarts = Plugin.showCarts.Value;

            if (!wantBoats && !wantCarts)
            {
                if (_tracked.Count > 0) Clear();
                return;
            }

            if (Minimap.instance == null || Player.m_localPlayer == null) return;
            if (!VehicleKinds.Ready) return;

            // Whether a pin carries a name is decided when it is built rather than written to it
            // after, so changing the setting throws the pins away and makes them again.
            if (_named != Plugin.showVehicleNames.Value)
            {
                _named = Plugin.showVehicleNames.Value;
                Clear();
            }

            VehicleRpc.Tick();

            Gather(wantBoats, wantCarts);
            Reconcile();
            UpdatePositions();
        }

        private static void Gather(bool wantBoats, bool wantCarts)
        {
            _wanted.Clear();

            var origin = Player.m_localPlayer.transform.position;
            var range = Plugin.explorationRadius.Value;
            var rangeSqr = range * range;

            foreach (var updater in Ship.Instances)
            {
                var ship = updater as Ship;
                if (ship == null) continue;

                Consider(ship.gameObject, wantBoats, wantCarts, origin, rangeSqr);
            }

            var carts = Carts();

            if (carts != null)
            {
                foreach (var cart in carts)
                {
                    if (cart == null) continue;

                    Consider(cart.gameObject, wantBoats, wantCarts, origin, rangeSqr);
                }
            }

            // Yours, wherever they are. One that is also loaded is already in from the pass above
            // and keeps its live position instead of the one the server last wrote.
            foreach (var record in VehicleRpc.Remote)
            {
                if (_wanted.ContainsKey(record.Id)) continue;

                var kind = VehicleKinds.Get(record.Prefab);
                if (kind == null) continue;
                if (kind.IsBoat ? !wantBoats : !wantCarts) continue;

                _wanted[record.Id] = new Vehicle
                {
                    Instance = null,
                    Pos = record.Pos,
                    Heading = record.Heading,
                    Kind = kind
                };
            }
        }

        private static void Consider(GameObject go, bool wantBoats, bool wantCarts, Vector3 origin, float rangeSqr)
        {
            var nview = go.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;

            var position = go.transform.position;
            if ((position - origin).sqrMagnitude > rangeSqr) return;

            var zdo = nview.GetZDO();

            var kind = VehicleKinds.Get(zdo.GetPrefab());
            if (kind == null) return;
            if (kind.IsBoat ? !wantBoats : !wantCarts) return;

            _wanted[zdo.m_uid] = new Vehicle
            {
                Instance = go.transform,
                Pos = position,
                Heading = go.transform.rotation.eulerAngles.y,
                Kind = kind
            };
        }

        private static void Reconcile()
        {
            var map = Minimap.instance;

            _gone.Clear();

            foreach (var pair in _tracked)
            {
                if (!_wanted.ContainsKey(pair.Key)) _gone.Add(pair.Key);
            }

            foreach (var id in _gone)
            {
                map.RemovePin(_tracked[id].Pin);
                _tracked.Remove(id);
            }

            foreach (var pair in _wanted)
            {
                var vehicle = pair.Value;

                if (_tracked.TryGetValue(pair.Key, out var existing))
                {
                    // One that has just come into range takes over from the position the server
                    // gave for it, and one that has left goes back to it.
                    existing.Instance = vehicle.Instance;

                    if (vehicle.Instance == null)
                    {
                        existing.Pin.m_pos = vehicle.Pos;
                        existing.Heading = vehicle.Heading;
                    }

                    continue;
                }

                var named = _named && !string.IsNullOrEmpty(vehicle.Kind.Label);

                var pin = map.AddPin(vehicle.Pos, Minimap.PinType.Icon0,
                    named ? vehicle.Kind.Label : "", save: false, isChecked: false);

                var icon = vehicle.Kind.IsBoat ? Icons.Boat : Icons.Cart;
                if (icon != null) pin.m_icon = icon;

                // Minimap.UpdatePins builds and places the label itself once this exists.
                if (named) pin.m_NamePinData = new Minimap.PinNameData(pin);

                _tracked[pair.Key] = new Tracked
                {
                    Pin = pin,
                    Instance = vehicle.Instance,
                    Heading = vehicle.Heading
                };
            }
        }

        /// <summary>
        /// The cheap pass, and the only one that runs on the real objects.
        /// </summary>
        private static void UpdatePositions()
        {
            var moved = false;

            foreach (var pair in _tracked)
            {
                var tracked = pair.Value;
                if (tracked.Instance == null) continue;

                var position = tracked.Instance.position;

                if (tracked.Pin.m_pos != position)
                {
                    tracked.Pin.m_pos = position;
                    moved = true;
                }

                tracked.Heading = tracked.Instance.rotation.eulerAngles.y;
            }

            if (moved) MapPins.RequestRedraw();
        }

        /// <summary>
        /// Heading, reapplied after Minimap.UpdatePins. A pin's marker is thrown away when it leaves
        /// the visible part of the map and built again from scratch on the way back, and the game has
        /// no notion of a rotated pin to rebuild, so the angle has to be put back here.
        /// </summary>
        public static void RestylePins()
        {
            if (_tracked.Count == 0 || !Plugin.rotateBoatIcons.Value) return;

            foreach (var pair in _tracked)
            {
                var tracked = pair.Value;
                if (tracked.Pin == null || tracked.Pin.m_uiElement == null) continue;

                // North is up on the map, and a heading turns clockwise while UI angles turn the
                // other way.
                tracked.Pin.m_uiElement.localRotation = Quaternion.Euler(0f, 0f, -tracked.Heading);
            }
        }

        public static void Clear()
        {
            if (Minimap.instance != null)
            {
                foreach (var pair in _tracked)
                {
                    if (pair.Value.Pin != null) Minimap.instance.RemovePin(pair.Value.Pin);
                }
            }

            _tracked.Clear();
            _wanted.Clear();
        }
    }
}
