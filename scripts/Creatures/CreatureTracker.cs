using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterMap.Scripts.Creatures
{
    /// <summary>
    /// Draws creatures near the player on the minimap and the map.
    ///
    /// Two passes at different rates. Which creatures have a pin changes slowly and is expensive to
    /// change, because it scans every loaded character and creates or destroys UI objects, so it
    /// runs on an interval. Where those pins are changes constantly and costs one assignment each,
    /// so it runs every frame: Minimap.UpdatePins recomputes screen position from m_pos anyway, and
    /// leaving m_pos stale between refreshes makes every creature visibly jump.
    /// </summary>
    public static class CreatureTracker
    {
        private class Tracked
        {
            public Character Creature;
            public Minimap.PinData Pin;
            public bool Tamed;
        }

        private static readonly Dictionary<Character, Tracked> _tracked = new Dictionary<Character, Tracked>();
        private static readonly List<Character> _leaving = new List<Character>();
        private static readonly List<Character> _characters = new List<Character>();

        private static float _nextRefresh;

        private static readonly Color TamedTint = new Color(0.45f, 0.95f, 0.45f, 1f);

        public static void Tick()
        {
            if (!Plugin.showCreatures.Value)
            {
                if (_tracked.Count > 0) Clear();
                return;
            }

            if (Minimap.instance == null || Player.m_localPlayer == null) return;

            if (Time.time >= _nextRefresh)
            {
                _nextRefresh = Time.time + Plugin.creatureRefreshInterval.Value;
                Refresh();
            }

            UpdatePositions();
        }

        // The expensive pass: decide which creatures should have a pin at all.
        private static void Refresh()
        {
            var origin = Player.m_localPlayer.transform.position;
            var range = Plugin.creatureRadius.Value;
            var rangeSqr = range * range;

            // Drop anything that died, was destroyed or walked out of range. A dead creature is not
            // returned by GetAllCharacters, but its entry here still holds a destroyed reference.
            _leaving.Clear();

            foreach (var pair in _tracked)
            {
                var creature = pair.Key;

                if (creature == null || creature.IsDead() ||
                    (creature.transform.position - origin).sqrMagnitude > rangeSqr)
                {
                    _leaving.Add(creature);
                }
            }

            foreach (var creature in _leaving)
            {
                Remove(creature);
            }

            _characters.Clear();
            _characters.AddRange(Character.GetAllCharacters());

            foreach (var creature in _characters)
            {
                if (_tracked.ContainsKey(creature)) continue;
                if (!ShouldTrack(creature)) continue;
                if ((creature.transform.position - origin).sqrMagnitude > rangeSqr) continue;

                Add(creature);
            }
        }

        // The cheap pass: one assignment per tracked creature, so they glide rather than step.
        private static void UpdatePositions()
        {
            foreach (var pair in _tracked)
            {
                if (pair.Key == null) continue;
                pair.Value.Pin.m_pos = pair.Key.transform.position;
            }
        }

        private static bool ShouldTrack(Character creature)
        {
            if (creature == null || creature.IsDead()) return false;

            // Other players are already handled by the game's own map sharing.
            if (creature.IsPlayer()) return false;

            // Summons that expire on their own: skeletons from a staff, the troll from a totem.
            if (creature.GetComponent<CharacterTimedDestruction>() != null) return false;

            // Boss spirits, which appear during a fight and are not creatures anyone hunts.
            if (CreatureIcons.PrefabName(creature.gameObject).StartsWith("Aspect_", StringComparison.Ordinal))
                return false;

            return true;
        }

        private static void Add(Character creature)
        {
            var icon = CreatureIcons.Get(creature);
            var tamed = creature.IsTamed();
            var given = GivenName(creature);

            // A name is only optional when the icon already says what this is. With no trophy the
            // name is the only identification, and a tame someone has named is the whole point of
            // tracking it, so both ignore the setting.
            var mustName = icon == null || (tamed && !string.IsNullOrEmpty(given));
            var wantsName = mustName || Plugin.showEntityNames.Value;

            var label = !string.IsNullOrEmpty(given)
                ? given
                : Localization.instance.Localize(creature.m_name);

            var pin = Minimap.instance.AddPin(
                creature.transform.position,
                Minimap.PinType.Icon3,
                wantsName ? label : "",
                save: false,
                isChecked: false);

            if (icon != null) pin.m_icon = icon;

            if (wantsName)
            {
                // Minimap.UpdatePins builds and positions the label itself once this exists, on
                // whichever of the two maps is open.
                pin.m_NamePinData = new Minimap.PinNameData(pin);
            }

            _tracked[creature] = new Tracked { Creature = creature, Pin = pin, Tamed = tamed };
        }

        private static void Remove(Character creature)
        {
            if (!_tracked.TryGetValue(creature, out var tracked)) return;

            _tracked.Remove(creature);

            if (Minimap.instance != null && tracked.Pin != null)
            {
                Minimap.instance.RemovePin(tracked.Pin);
            }
        }

        private static string GivenName(Character creature)
        {
            var tameable = creature.GetComponent<Tameable>();
            if (tameable == null) return null;

            var text = tameable.GetText();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        /// <summary>
        /// Reapplied after Minimap.UpdatePins, which writes every pin's colour on every frame and
        /// would otherwise wash the tint straight back out.
        /// </summary>
        public static void ApplyTints()
        {
            if (!Plugin.tintTamedCreatures.Value || _tracked.Count == 0) return;

            foreach (var pair in _tracked)
            {
                var tracked = pair.Value;
                if (!tracked.Tamed) continue;

                var icon = tracked.Pin?.m_iconElement;
                if (icon != null) icon.color = TamedTint;
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
        }
    }
}
