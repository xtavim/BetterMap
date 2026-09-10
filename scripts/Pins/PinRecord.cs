using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BetterMap.Scripts.Pins
{
    /// <summary>
    /// Where we have already pinned.
    ///
    /// The question before placing a pin is asked here and never of the map. Those are different
    /// questions the moment a player deletes one of our pins: the map says there is nothing there,
    /// this says we put something there once and were told to take it away. Walking past again must
    /// not put it back.
    ///
    /// It also does the work of not stacking pins on one another, since the same deposit is nought
    /// metres from itself and so is caught by the same distance check as its neighbours.
    ///
    /// Kept on the character, per world, alongside the death markers and the vehicles driven.
    /// </summary>
    public static class PinRecord
    {
        private const string CustomDataKey = "BetterMap.pinned";

        private struct Entry
        {
            public PinCategory Category;
            public float X;
            public float Z;
        }

        // Bucketed by a coarse grid so a world with thousands of pins does not turn every check into
        // a walk of the whole list. Cells are wide enough that a match can only be in the nine
        // around the point.
        private const float CellSize = 32f;

        private static readonly Dictionary<long, List<Entry>> _cells = new Dictionary<long, List<Entry>>();

        private static long _world;
        private static bool _loaded;
        private static bool _dirty;

        public static int Count { get; private set; }

        public static bool Has(PinCategory category, Vector3 position, float within)
        {
            Load();
            if (!_loaded) return false;

            var withinSqr = within * within;

            var cx = Mathf.FloorToInt(position.x / CellSize);
            var cz = Mathf.FloorToInt(position.z / CellSize);

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dz = -1; dz <= 1; dz++)
                {
                    if (!_cells.TryGetValue(Key(cx + dx, cz + dz), out var entries)) continue;

                    foreach (var entry in entries)
                    {
                        if (entry.Category != category) continue;

                        var ex = entry.X - position.x;
                        var ez = entry.Z - position.z;

                        if (ex * ex + ez * ez <= withinSqr) return true;
                    }
                }
            }

            return false;
        }

        public static void Add(PinCategory category, Vector3 position)
        {
            Load();
            if (!_loaded) return;

            Put(new Entry { Category = category, X = position.x, Z = position.z });

            Count++;
            _dirty = true;
        }

        /// <summary>
        /// Written out in one go rather than on every pin, because a walk through new ground places
        /// them in bursts and the string is rebuilt whole each time.
        /// </summary>
        public static void Flush()
        {
            if (!_dirty || !_loaded || Player.m_localPlayer == null) return;

            _dirty = false;

            var parts = new List<string>();

            // Other worlds' entries are carried through untouched: one character can visit several.
            if (Player.m_localPlayer.m_customData.TryGetValue(CustomDataKey, out var text) && !string.IsNullOrEmpty(text))
            {
                foreach (var entry in text.Split('|'))
                {
                    var head = entry.IndexOf(';');
                    if (head <= 0) continue;

                    if (!long.TryParse(entry.Substring(0, head), NumberStyles.Integer, CultureInfo.InvariantCulture, out var world)) continue;
                    if (world == _world) continue;

                    parts.Add(entry);
                }
            }

            foreach (var cell in _cells.Values)
            {
                foreach (var entry in cell)
                {
                    parts.Add(string.Join(";", new[]
                    {
                        _world.ToString(CultureInfo.InvariantCulture),
                        ((int)entry.Category).ToString(CultureInfo.InvariantCulture),
                        entry.X.ToString("0.#", CultureInfo.InvariantCulture),
                        entry.Z.ToString("0.#", CultureInfo.InvariantCulture)
                    }));
                }
            }

            Player.m_localPlayer.m_customData[CustomDataKey] = string.Join("|", parts.ToArray());
        }

        /// <summary>
        /// Throws away everything this character remembers pinning, so it all gets pinned again.
        /// There to test with; a player has no reason for it, and it undoes every pin they deleted.
        /// </summary>
        public static void Wipe()
        {
            Load();
            if (!_loaded) return;

            _cells.Clear();
            Count = 0;
            _dirty = true;

            Flush();

            Plugin.Logger.LogInfo("PinRecord: wiped");
        }

        public static void Forget()
        {
            _cells.Clear();
            _loaded = false;
            _dirty = false;
            _world = 0L;
            Count = 0;
        }

        private static void Load()
        {
            if (Player.m_localPlayer == null || ZNet.instance == null) return;

            var world = ZNet.instance.GetWorldUID();
            if (_loaded && _world == world) return;

            _cells.Clear();
            _world = world;
            _loaded = true;
            _dirty = false;
            Count = 0;

            if (!Player.m_localPlayer.m_customData.TryGetValue(CustomDataKey, out var text)) return;
            if (string.IsNullOrEmpty(text)) return;

            foreach (var entry in text.Split('|'))
            {
                var parts = entry.Split(';');
                if (parts.Length != 4) continue;

                if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryWorld)) continue;
                if (entryWorld != world) continue;

                if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var category)) continue;
                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)) continue;
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)) continue;

                Put(new Entry { Category = (PinCategory)category, X = x, Z = z });
                Count++;
            }

            if (Plugin.debugMode.Value) Plugin.Logger.LogInfo($"PinRecord: {Count} places already pinned");
        }

        private static void Put(Entry entry)
        {
            var key = Key(Mathf.FloorToInt(entry.X / CellSize), Mathf.FloorToInt(entry.Z / CellSize));

            if (!_cells.TryGetValue(key, out var entries))
            {
                entries = new List<Entry>();
                _cells[key] = entries;
            }

            entries.Add(entry);
        }

        private static long Key(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }
    }
}
