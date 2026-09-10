using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Map
{
    /// <summary>
    /// Death markers that survive a reload.
    ///
    /// The game already drops a pin where you died, but it never reaches the save file: the writer
    /// skips every pin of type Death, so the markers are gone the next time the world is loaded,
    /// and within a session they pile up with no limit. The positions are kept here instead, on the
    /// character, and the pins are rebuilt from them.
    ///
    /// This owns every Death pin on the map, so the game's marker and ours cannot end up stacked on
    /// the same spot. Nothing else creates one: the map refuses that type when a pin is placed by
    /// hand, and the only other source, the automatic pin for the last death recorded on the
    /// profile, is switched off in the game itself.
    /// </summary>
    public static class DeathMarkers
    {
        // Lives in the character's custom data, which is written out with the character, so markers
        // follow whoever died rather than the world. Each entry still records the world it happened
        // in: the same character carries its deaths into every world it visits.
        private const string CustomDataKey = "BetterMap.deaths";

        private struct Marker
        {
            public long World;
            public Vector3 Pos;
            public string Label;
        }

        private static readonly AccessTools.FieldRef<Minimap, List<Minimap.PinData>> MapPins =
            AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>("m_pins");

        private static bool _rebuilt;

        public static void Tick()
        {
            if (Player.m_localPlayer == null)
            {
                // Out of the world. The next one rebuilds from its own character.
                _rebuilt = false;
                return;
            }

            if (_rebuilt || Minimap.instance == null) return;

            _rebuilt = true;
            Rebuild();
        }

        /// <summary>
        /// Loading map data clears every pin on the map, and it happens on either side of the
        /// player being ready depending on how long the world takes to come up. Rather than order
        /// the two, the rebuild is simply asked for again.
        /// </summary>
        public static void Invalidate()
        {
            _rebuilt = false;
        }

        public static void Record(Vector3 pos)
        {
            if (Player.m_localPlayer == null || ZNet.instance == null) return;

            var markers = Load();

            markers.Add(new Marker
            {
                World = ZNet.instance.GetWorldUID(),
                Pos = pos,
                Label = DayLabel()
            });

            Store(markers);
            Rebuild();
        }

        /// <summary>
        /// The same text the game puts on its own death pin, composed here rather than read back
        /// off that pin so a marker is still labelled if anything stops the game adding it.
        /// </summary>
        private static string DayLabel()
        {
            if (EnvMan.instance == null || ZNet.instance == null) return "";

            return $"$hud_mapday {EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds())}";
        }

        private static void Rebuild()
        {
            var map = Minimap.instance;
            if (map == null || Player.m_localPlayer == null || ZNet.instance == null) return;

            var pins = MapPins(map);

            // Backwards: RemovePin takes them out of this same list.
            for (var i = pins.Count - 1; i >= 0; i--)
            {
                if (pins[i].m_type == Minimap.PinType.Death) map.RemovePin(pins[i]);
            }

            var world = ZNet.instance.GetWorldUID();

            foreach (var marker in Trim(Load()))
            {
                if (marker.World != world) continue;

                // Not saved with the map: the game would drop it on the way out anyway, and these
                // are rebuilt from the character instead.
                map.AddPin(marker.Pos, Minimap.PinType.Death, marker.Label, save: false, isChecked: false);
            }
        }

        /// <summary>
        /// Keeps the newest few of each world. Counting per world rather than overall stops a run
        /// of deaths in one world from wiping the markers left in another.
        /// </summary>
        private static List<Marker> Trim(List<Marker> markers)
        {
            var keep = Plugin.deathMarkersKept.Value;
            var counts = new Dictionary<long, int>();
            var kept = new List<Marker>();

            for (var i = markers.Count - 1; i >= 0; i--)
            {
                var marker = markers[i];

                counts.TryGetValue(marker.World, out var seen);
                if (seen >= keep) continue;

                counts[marker.World] = seen + 1;
                kept.Add(marker);
            }

            kept.Reverse();
            return kept;
        }

        private static List<Marker> Load()
        {
            var markers = new List<Marker>();

            if (Player.m_localPlayer == null) return markers;
            if (!Player.m_localPlayer.m_customData.TryGetValue(CustomDataKey, out var text)) return markers;
            if (string.IsNullOrEmpty(text)) return markers;

            foreach (var entry in text.Split('|'))
            {
                // The label is whatever is left, so a separator inside it cannot split the entry.
                var parts = entry.Split(new[] { ';' }, 5);
                if (parts.Length < 5) continue;

                if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var world)) continue;
                if (!TryFloat(parts[1], out var x)) continue;
                if (!TryFloat(parts[2], out var y)) continue;
                if (!TryFloat(parts[3], out var z)) continue;

                markers.Add(new Marker { World = world, Pos = new Vector3(x, y, z), Label = parts[4] });
            }

            return markers;
        }

        private static void Store(List<Marker> markers)
        {
            if (Player.m_localPlayer == null) return;

            var parts = new List<string>();

            foreach (var marker in Trim(markers))
            {
                parts.Add(string.Join(";", new[]
                {
                    marker.World.ToString(CultureInfo.InvariantCulture),
                    Float(marker.Pos.x),
                    Float(marker.Pos.y),
                    Float(marker.Pos.z),
                    marker.Label
                }));
            }

            // Left for the game's own save to carry out, which happens on the way to respawning.
            Player.m_localPlayer.m_customData[CustomDataKey] = string.Join("|", parts.ToArray());
        }

        private static string Float(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static bool TryFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
