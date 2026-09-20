using System.Collections.Generic;
using System.Globalization;

namespace BetterMap.Scripts.Vehicles
{
    // A ZDOID holds its owner as an index into a list the game rebuilds on every world load, so one
    // kept across a reload reads an owner that is no longer there.
    public readonly struct Owned : System.IEquatable<Owned>
    {
        public readonly long User;
        public readonly uint Id;

        public Owned(long user, uint id)
        {
            User = user;
            Id = id;
        }

        public bool Equals(Owned other) => User == other.User && Id == other.Id;

        public override bool Equals(object other) => other is Owned owned && Equals(owned);

        public override int GetHashCode() => (User.GetHashCode() * 397) ^ (int)Id;
    }

    public static class VehicleMemory
    {
        private const string CustomDataKey = "BetterMap.vehicles";

        private static readonly HashSet<Owned> _used = new HashSet<Owned>();

        private static long _world;
        private static bool _loaded;

        public static void Remember(ZDOID id)
        {
            if (id == ZDOID.None) return;

            Load();
            if (!_loaded) return;

            if (!_used.Add(new Owned(id.UserID, id.ID))) return;

            Store();

            if (Plugin.debugMode.Value) Plugin.Logger.LogInfo($"VehicleMemory: remembered {id}");
        }

        public static HashSet<Owned> Used()
        {
            Load();
            return _used;
        }

        public static void Forget()
        {
            _used.Clear();
            _loaded = false;
            _world = 0L;
        }

        private static void Load()
        {
            if (Player.m_localPlayer == null || ZNet.instance == null) return;

            var world = ZNet.instance.GetWorldUID();
            if (_loaded && _world == world) return;

            _used.Clear();
            _world = world;
            _loaded = true;

            if (!Player.m_localPlayer.m_customData.TryGetValue(CustomDataKey, out var text)) return;
            if (string.IsNullOrEmpty(text)) return;

            foreach (var entry in text.Split('|'))
            {
                var parts = entry.Split(';');
                if (parts.Length != 3) continue;

                if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryWorld)) continue;
                if (entryWorld != world) continue;

                if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var user)) continue;
                if (!uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)) continue;

                _used.Add(new Owned(user, id));
            }
        }

        private static void Store()
        {
            if (Player.m_localPlayer == null) return;

            var kept = new List<string>();

            if (Player.m_localPlayer.m_customData.TryGetValue(CustomDataKey, out var text) && !string.IsNullOrEmpty(text))
            {
                foreach (var entry in text.Split('|'))
                {
                    var parts = entry.Split(';');
                    if (parts.Length != 3) continue;

                    if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryWorld)) continue;
                    if (entryWorld == _world) continue;

                    kept.Add(entry);
                }
            }

            foreach (var id in _used)
            {
                kept.Add(string.Join(";", new[]
                {
                    _world.ToString(CultureInfo.InvariantCulture),
                    id.User.ToString(CultureInfo.InvariantCulture),
                    id.Id.ToString(CultureInfo.InvariantCulture)
                }));
            }

            Player.m_localPlayer.m_customData[CustomDataKey] = string.Join("|", kept.ToArray());
        }
    }
}
