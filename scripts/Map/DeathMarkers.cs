using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Map
{
    public static class DeathMarkers
    {
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
                _rebuilt = false;
                return;
            }

            if (_rebuilt || Minimap.instance == null) return;

            _rebuilt = true;
            Rebuild();
        }

        public static void Invalidate()
        {
            _rebuilt = false;
        }

        // The game's own death pin never reaches the save file: the writer skips every pin of type
        // Death, so the positions are kept here instead.
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

            for (var i = pins.Count - 1; i >= 0; i--)
            {
                if (pins[i].m_type == Minimap.PinType.Death) map.RemovePin(pins[i]);
            }

            var world = ZNet.instance.GetWorldUID();

            foreach (var marker in Trim(Load()))
            {
                if (marker.World != world) continue;

                map.AddPin(marker.Pos, Minimap.PinType.Death, marker.Label, save: false, isChecked: false);
            }
        }

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
