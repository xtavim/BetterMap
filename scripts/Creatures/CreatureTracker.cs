using System;
using System.Collections.Generic;
using BetterMap.Scripts;
using UnityEngine;

namespace BetterMap.Scripts.Creatures
{
    public static class CreatureTracker
    {
        private class Tracked
        {
            public Character Creature;
            public Minimap.PinData Pin;
            public bool Tamed;
            public bool Named;
            public string Label;
            public bool Hostile;
            public bool HasIcon;
        }

        private struct Style
        {
            public Sprite Icon;
            public bool Tamed;
            public bool Named;
            public string Label;
            public bool Hostile;
        }

        private static readonly Dictionary<Character, Tracked> _tracked = new Dictionary<Character, Tracked>();
        private static readonly List<Character> _leaving = new List<Character>();
        private static readonly List<Character> _characters = new List<Character>();

        private static float _nextRefresh;

        private static readonly HashSet<string> Excluded = new HashSet<string>
        {
            "piece_TrainingDummy",
            "FrozenKing_p2",
            "FrozenKing_p3"
        };

        private static readonly Color TamedTint = new Color(0.45f, 0.95f, 0.45f, 1f);
        private static readonly Color HostileTint = new Color(0.95f, 0.4f, 0.4f, 1f);

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

        private static void Refresh()
        {
            var origin = Player.m_localPlayer.transform.position;
            var range = Plugin.explorationRadius.Value;
            var rangeSqr = range * range;

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

            _leaving.Clear();

            foreach (var pair in _tracked)
            {
                if (pair.Key == null) continue;

                var style = Describe(pair.Key);
                var tracked = pair.Value;

                tracked.Hostile = style.Hostile;

                if (style.Tamed != tracked.Tamed || style.Named != tracked.Named ||
                    (style.Named && style.Label != tracked.Label))
                {
                    _leaving.Add(pair.Key);
                }
            }

            foreach (var creature in _leaving)
            {
                Remove(creature);
                Add(creature);
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

        private static void UpdatePositions()
        {
            var moved = false;

            foreach (var pair in _tracked)
            {
                if (pair.Key == null) continue;

                var position = pair.Key.transform.position;
                if (pair.Value.Pin.m_pos == position) continue;

                pair.Value.Pin.m_pos = position;
                moved = true;
            }

            if (moved) MapPins.RequestRedraw();
        }

        private static bool ShouldTrack(Character creature)
        {
            if (creature == null || creature.IsDead()) return false;

            if (creature.IsPlayer()) return false;

            if (creature.GetComponent<CharacterTimedDestruction>() != null) return false;

            var prefab = CreatureIcons.PrefabName(creature.gameObject);

            if (prefab.StartsWith("Aspect_", StringComparison.Ordinal)) return false;

            if (Excluded.Contains(prefab)) return false;

            return true;
        }

        private static Style Describe(Character creature)
        {
            var icon = CreatureIcons.Get(creature);
            var tamed = creature.IsTamed();
            var given = GivenName(creature);

            var mustName = icon == null || (tamed && !string.IsNullOrEmpty(given));
            var named = mustName || Plugin.showEntityNames.Value;

            return new Style
            {
                Icon = icon,
                Tamed = tamed,
                Named = named,
                Hostile = IsHostile(creature, tamed),

                Label = !named
                    ? null
                    : !string.IsNullOrEmpty(given)
                        ? given
                        : Localization.instance.Localize(creature.m_name)
            };
        }

        private static void Add(Character creature)
        {
            var style = Describe(creature);

            var pin = Minimap.instance.AddPin(
                creature.transform.position,
                Pins.PinLegend.CreatureType,
                style.Named ? style.Label : "",
                save: false,
                isChecked: false);

            if (style.Icon != null) pin.m_icon = style.Icon;

            if (style.Named)
            {
                pin.m_NamePinData = new Minimap.PinNameData(pin);
            }

            _tracked[creature] = new Tracked
            {
                Creature = creature,
                Pin = pin,
                Tamed = style.Tamed,
                Named = style.Named,
                Label = style.Label,
                Hostile = style.Hostile,
                HasIcon = style.Icon != null
            };
        }

        private static bool IsHostile(Character creature, bool tamed)
        {
            if (tamed) return false;

            var ai = creature.GetBaseAI() as MonsterAI;
            if (ai == null) return false;

            if (ai.m_aggravatable && !ai.IsAggravated()) return false;

            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs))
                return false;

            return true;
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

        public static void RestylePins()
        {
            if (_tracked.Count == 0 || Minimap.instance == null) return;

            var tintTamed = Plugin.tintTamedCreatures.Value;
            var tintHostile = Plugin.tintHostileCreatures.Value;

            foreach (var pair in _tracked)
            {
                var tracked = pair.Value;
                var pin = tracked.Pin;
                if (pin?.m_iconElement == null) continue;

                if (tracked.Tamed)
                {
                    if (tintTamed) pin.m_iconElement.color = TamedTint;
                }
                else if (tintHostile && tracked.Hostile && !tracked.HasIcon)
                {
                    pin.m_iconElement.color = HostileTint;
                }
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
