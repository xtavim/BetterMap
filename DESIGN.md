# BetterMap — design

A minimap and map overhaul for Valheim 1.0.7. Native only, no Jotunn: everything
it needs is public on `Minimap`, `Character`, `ZNetScene` and `ZDOMan`, and taking
Jotunn would mean inheriting a hard dependency for no gain.

Every feature is a BepInEx config entry, synchronised with ServerSync.

## Guiding principle

Derive from game data, never from hardcoded tables. Creature icons come from the
creature's own drop list, resources are classified by the components they carry.
Content added by Valheim updates and by other mods is then handled without any
maintenance. The one unavoidable exception is the vehicle prefab names.

## Creatures

Transient pins (`m_save = false`) rebuilt from world state, shown on both the
minimap and the full map, since both draw from the same pin list.

| | Icon | Name |
| --- | --- | --- |
| Hostile, has trophy | trophy sprite | config |
| Hostile, no trophy | default pin | always |
| Tamed, has trophy, unnamed | trophy sprite, green | config |
| Tamed, has trophy, named | trophy sprite, green | always |
| Tamed, no trophy | circle pin, green | always |

A name is only optional when the icon already identifies the creature. Where the
name is the only identification, or is a name the player chose, it always shows.

- The trophy sprite comes from `CharacterDrop.m_drops`, the entry whose prefab is
  named `Trophy*`, then `ItemDrop.m_itemData.m_shared.m_icons[0]`. Cached per
  prefab, never resolved per frame.
- The green tint has to be reapplied after `Minimap.UpdatePins`, which writes
  `m_iconElement.color` every frame.
- A tame's chosen name is on the ZDO as `ZDOVars.s_tamedName`. Note some tames get
  a random starting name they were never given by the player.
- Tracking is limited to a radius. Outside it, creatures are not drawn and not
  processed at all.

### Two cadences

Which creatures have pins changes slowly and is expensive to change: it scans
every loaded character and creates or destroys UI objects. Where those pins are
changes constantly and costs one `Vector3` write each.

So the membership pass runs on an interval and the position pass runs every
frame. `Minimap.UpdatePins` already recomputes screen position from `m_pos` every
frame, so keeping `m_pos` fresh costs nothing extra and stops pins from visibly
stepping between refreshes.

### Death

A creature that dies must lose its pin. `Character.GetAllCharacters()` will not
return it, but the destroyed object leaves a dangling entry in the tracking
dictionary, so the membership pass has to prune null and dead entries rather than
relying on the radius check alone.

## Vehicles

Boats and carts, wherever they are in the world rather than only near the player,
because the point is finding a boat that drifted off.

- Loaded vehicles come from `Ship.Instances` and `Vagon.m_instances` (private,
  needs a field ref).
- Unloaded ones come from `ZDOMan.GetAllZDOsWithPrefabIterative`, which is
  designed to be spread over frames, so this runs on a slow interval.
- Boat icons are rotated to the boat's heading.
- This is the one feature that needs a prefab name list.

## Auto pins

Persistent pins, curated in PINS.md, with icons of our own. A saved pin persists
`name, pos, type, checked, ownerID, author` and no sprite, so the icon is put
back at load time by looking the pin's name up among the ones we place. That is
safe because Valheim cannot rename a pin: placing one asks for a name and makes a
new pin, and clicking an existing one only ticks it off.

- What to pin is a curated list, not a component sweep. Detection matches the
  prefab and then asks `Heightmap.FindBiome` where the thing stands, because the
  same berry can be worth pinning in one biome and not another.
- Scattered objects are found by sweeping what the game has loaded. Places, the
  crypts and caves and fortresses, are assembled differently and need their own
  mechanism.
- **Pins are never removed automatically**, and whether to place one is asked of
  our own record of where we have already pinned, never of the pins on the map.
  Those are different questions the moment a player deletes one of ours: the map
  says nothing is there, the record says we put something there and were told to
  take it away. The record also does the work of not stacking pins, since the
  same deposit is nought metres from itself.

### Named pins, still to do

Three cases where a pin should carry a name it can only get from somewhere else.

- **Traders.** The game pins a trader itself, through `m_locationIcons`, and the
  pin arrives with no name. It should carry the trader's, so a map with Haldor
  and the bog witch on it says which is which.
- **Portals, on placing.** Dropping a portal should pin it straight away, with
  the vanilla portal icon, which already exists and needs nothing drawn.
- **Portals, on tagging.** Confirming the Set Tag screen should put that tag on
  the pin already standing there. Editing the pin in place is the tidy way;
  removing it and adding it again is acceptable and is what the death markers
  already do, since nothing about a pin can be changed once its marker is built.

## Map

- Keep the last N death markers instead of vanilla's one.
- Configurable exploration radius, defaulting to the vanilla value.

## Storage

Map data lives on the player profile, not the world
(`Game.instance.GetPlayerProfile().SetMapData(...)`). Pins therefore follow the
character, and a second character in the same world starts with an empty map.
That is vanilla behaviour and it stays that way: the cartography table is the
game's own answer to sharing.

## Deferred

- **Pin search** on the large map, for 1.1. `Minimap.CenterMap` exists but is
  private. It is the only feature here that needs real UI, which is also the only
  argument for taking Jotunn.

## Open until we have real data

- Which resources are worth pinning. To be decided from a one-off dump of
  `ZNetScene.instance.m_prefabs` filtered by the components above, rather than
  from a wiki.
- Whether trophy icons are legible at pin size. If they are not, the whole visual
  concept changes and names become more important.
