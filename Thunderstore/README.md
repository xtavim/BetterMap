# BetterMap

Puts creatures, boats, carts and resources on your map. Pins what you find as you find it, keeps your death markers, and names the traders. Everything can be turned on and off, biome by biome, and syncs with ServerSync.

![The map, with creatures, resources and dungeons pinned](https://raw.githubusercontent.com/xtavim/BetterMap/main/docs/map.jpg)

![The minimap](https://raw.githubusercontent.com/xtavim/BetterMap/main/docs/minimap.png)

## Features

### 🐗 Creatures

Every creature near you shows up on the map, drawn as its own trophy. A boar looks like a boar, a wolf looks like a wolf.

- Creatures without a trophy get a plain pin with their name under it, so you always know what is out there
- Tamed animals are tinted green, and a named pet shows its name
- Anything that will attack you on sight is tinted red

### ⛵ Boats and carts

Boats and carts you built, or have driven, show up wherever you left them, no matter how far away. Boats point the way they are facing.

Anyone else's boat only shows when you are close enough to see it.

### ⛏️ Resources

Ore, berries, seeds, tar pits, beehives, crypts, caves and more get pinned as you come across them. There is a checkbox for every single one, grouped by biome, so you decide what is worth marking.

Out of the box it pins the things worth a trip and leaves the clutter alone: copper, tin, silver, obsidian, flametal, gold, the crypts and caves, tar pits, vegvisirs, the fuling totem and the rarer seeds.

**A pin you delete stays deleted.** Walking past the same place again will not put it back.

### 💀 Death markers

The game drops a marker where you died but never saves it, so it is gone the next time you load. BetterMap keeps them, and keeps more than one.

### 🧭 Traders

Haldor, Hildir and the bog witch get their names on the map instead of an unlabelled icon.

If you want, BetterMap can also put **every trader in the world** on your map at once, so you never have to go hunting for Haldor again. This is off by default.

### 📍 Portals

Portals get pinned where you place them, named by their tag. Rename a portal and its pin follows.

### 🗺️ Map

- The pin legend gets a row for each of the new icons, so you can place them by hand and hide them with a right click, same as the game's own
- Pin icons can be made bigger
- The map uncovers as much ground as you want it to

## Installation

Install with your mod manager, or drop `BetterMap.dll` into `BepInEx/plugins`.

## Configuration

Everything below can be changed in-game with Configuration Manager, or in `BepInEx/config/xtav1m.BetterMap.cfg`.

### Creatures

| Setting | Default | What it does |
| --- | --- | --- |
| Show Creatures | On | Creatures on the map, drawn as their trophy |
| Creature Refresh Interval | 0.5s | How often the list of tracked creatures is rebuilt |
| Show Creature Names | Off | A name under every creature. Creatures with no trophy, and named pets, always show theirs |
| Tint Tamed Creatures | On | Tames are green |
| Tint Hostile Creatures | On | Creatures that attack on sight are red |

### Vehicles

| Setting | Default | What it does |
| --- | --- | --- |
| Show Boats | On | Boats on the map |
| Show Carts | On | Carts, sleds, battering rams and catapults |
| Rotate Boat Icons | On | Boats point the way they are facing |
| Show Vehicle Names | On | The name under each one |
| Vehicle Refresh Interval | 5s | How often we ask the server where your vehicles are |

### Auto Pins

| Setting | Default | What it does |
| --- | --- | --- |
| Enable | On | Pin resources as you come across them |
| Sweep Interval | 2s | How often we look around for something to pin |
| Merge Distance | 5m | How close two pins of the same kind may be |
| Pin Portals | On | Portals, named by their tag |
| Name Trader Pins | On | The trader's name on the pin the game already places |
| Reveal Traders | **Off** | Every trader in the world on your map at once |

Then one section per biome, **Auto Pins - Meadows** through **Auto Pins - Ocean**, with a checkbox for every resource, dungeon and landmark in it.

### Map

| Setting | Default | What it does |
| --- | --- | --- |
| Death Markers Kept | 3 | How many of your death markers to keep |
| Exploration Radius | 100m | How much ground the map uncovers as you walk. Also how far creatures and resources are spotted |
| Icon Scale | 1.25 | The size of every pin on the map |

## Compatibility

- Built against **Valheim 1.0** (build 25185596).
- Requires **BepInExPack Valheim 5.4.2350** or newer.
- Install on both the client and the server. Settings are synchronized with ServerSync, and the server's values take precedence while **Lock Configuration** is enabled.
- Finding your boats and carts anywhere in the world needs the mod on the server. Without it you still see the ones near you.
- Removing the mod does not break your map. Pins it placed stay where they are and go back to a plain icon.

## Changelog

See [CHANGELOG.md](changelog) for version history.

## Credits

- **ServerSync** - server-authoritative configuration syncing, merged into the plugin DLL.

## License

This mod is provided as-is for the Valheim community.
