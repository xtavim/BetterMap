using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts.Creatures
{
    /// <summary>
    /// Draws creatures near the player on the minimap and the map.
    ///
    /// Creatures are tracked within the radius the map uncovers as you walk, so a pin never sits on
    /// ground that is still black.
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
            public bool Named;
            public string Label;
            public bool Hostile;
            public bool HasIcon;
        }

        /// <summary>
        /// What a creature's pin should show right now.
        ///
        /// None of it is fixed for the creature's lifetime: it can be tamed, a tame can be named or
        /// renamed, and the name setting can be toggled while the pin is on screen. Taming and
        /// renaming do not recreate the creature, so nothing rebuilds the pin on its own.
        /// </summary>
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

        // UpdatePins only runs when Minimap decides a redraw is needed. Moving the player sets that
        // every frame, but standing still sets nothing, so pins would only be redrawn by our own
        // refresh and creatures visibly stepped twice a second. Vanilla's player pins raise the same
        // flag when a position changes, and this does the same.
        private static readonly AccessTools.FieldRef<Minimap, bool> PinUpdateRequired =
            AccessTools.FieldRefAccess<Minimap, bool>("m_pinUpdateRequired");

        // Oddities that are Characters but not creatures anyone tracks: a build piece, and the
        // later phases of a boss that are spawned as separate characters mid fight.
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

        // The expensive pass: decide which creatures should have a pin at all.
        private static void Refresh()
        {
            var origin = Player.m_localPlayer.transform.position;
            var range = Plugin.explorationRadius.Value;
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

            // Rebuild any pin whose creature no longer matches what the pin is showing. Without
            // this a pin only picks up a tame or a rename when the creature happens to walk out of
            // range and back in, which is what forces the marker to be built again.
            _leaving.Clear();

            foreach (var pair in _tracked)
            {
                if (pair.Key == null) continue;

                var style = Describe(pair.Key);
                var tracked = pair.Value;

                // Whether something is hostile can change under a pin that is otherwise correct,
                // when a dvergr is provoked. Only the colour depends on it and that is written on
                // every pass, so this is picked up in place rather than by rebuilding the pin.
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

        // The cheap pass: one assignment per tracked creature, so they glide rather than step.
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

            if (moved) PinUpdateRequired(Minimap.instance) = true;
        }

        private static bool ShouldTrack(Character creature)
        {
            if (creature == null || creature.IsDead()) return false;

            // Other players are already handled by the game's own map sharing.
            if (creature.IsPlayer()) return false;

            // Summons that expire on their own: skeletons from a staff, the troll from a totem.
            if (creature.GetComponent<CharacterTimedDestruction>() != null) return false;

            var prefab = CreatureIcons.PrefabName(creature.gameObject);

            // Boss spirits, which appear during a fight and are not creatures anyone hunts.
            if (prefab.StartsWith("Aspect_", StringComparison.Ordinal)) return false;

            if (Excluded.Contains(prefab)) return false;

            return true;
        }

        private static Style Describe(Character creature)
        {
            var icon = CreatureIcons.Get(creature);
            var tamed = creature.IsTamed();
            var given = GivenName(creature);

            // A name is only optional when the icon already says what this is. With no trophy the
            // name is the only identification, and a tame someone has named is the whole point of
            // tracking it, so both ignore the setting.
            var mustName = icon == null || (tamed && !string.IsNullOrEmpty(given));
            var named = mustName || Plugin.showEntityNames.Value;

            return new Style
            {
                Icon = icon,
                Tamed = tamed,
                Named = named,
                Hostile = IsHostile(creature, tamed),

                // Left null when no name is drawn, which also keeps the localiser out of the
                // refresh pass for the ordinary case of an icon and no name.
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
                Minimap.PinType.Icon3,
                style.Named ? style.Label : "",
                save: false,
                isChecked: false);

            if (style.Icon != null) pin.m_icon = style.Icon;

            if (style.Named)
            {
                // Minimap.UpdatePins builds and positions the label itself once this exists, on
                // whichever of the two maps is open.
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

        /// <summary>
        /// Whether this creature will come after the player.
        ///
        /// Not BaseAI.IsEnemy: that answers whether two creatures fight, and from the player's side
        /// everything that is not tamed or a dvergr comes back true, deer included. What separates
        /// them is the AI they were built with. AnimalAI only flees and has no notion of a target,
        /// so anything carrying it is harmless; MonsterAI hunts, unless it is one of the creatures
        /// that waits to be provoked and has not been.
        /// </summary>
        private static bool IsHostile(Character creature, bool tamed)
        {
            if (tamed) return false;

            var ai = creature.GetBaseAI() as MonsterAI;
            if (ai == null) return false;

            if (ai.m_aggravatable && !ai.IsAggravated()) return false;

            // The world modifier that stops creatures attacking at all.
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

        /// <summary>
        /// Size and colour, reapplied after Minimap.UpdatePins.
        ///
        /// Neither survives on its own. UpdatePins writes every pin's colour on every pass, so a
        /// tint set once is washed straight back out, and it sizes a pin only in the frame it
        /// builds the marker, from a size the game picks. Both have to be set here to hold.
        /// </summary>
        public static void RestylePins()
        {
            if (_tracked.Count == 0 || Minimap.instance == null) return;

            var map = Minimap.instance;
            var size = (map.m_mode == Minimap.MapMode.Large ? map.m_pinSizeLarge : map.m_pinSizeSmall)
                       * Plugin.creatureIconScale.Value;

            var tintTamed = Plugin.tintTamedCreatures.Value;
            var tintHostile = Plugin.tintHostileCreatures.Value;

            foreach (var pair in _tracked)
            {
                var tracked = pair.Value;
                var pin = tracked.Pin;
                if (pin?.m_uiElement == null) continue;

                if (!Mathf.Approximately(pin.m_uiElement.rect.width, size))
                {
                    pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                    pin.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                }

                if (pin.m_iconElement == null) continue;

                // Anything left alone keeps the white UpdatePins just gave it, which is what a
                // creature that is neither tamed nor out for blood should look like.
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
