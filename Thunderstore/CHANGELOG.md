# Changelog

## [1.0.5] - 2026-09-20

### Added

- **Infested mines** in the Mistlands and **putrid holes** in the Ashlands are pinned.

### Fixed

- A vegvisir pin no longer appears at ruins that do not actually have one.
- Pin icons no longer clash with mods that add their own, such as TargetPortal.
- No more repeated errors after loading a world a second time in the same session.

## [1.0.4] - 2026-09-20

### Fixed

- **Vegvisirs are pinned.** They never were. They are built into the ruins that hold them rather
  than placed as objects of their own, so the sweep that finds everything else could not see them.

### Added

- **Frost caves** are pinned, on by default.

### Changed

- **Obsidian deposits** ship unticked.

## [1.0.3] - 2026-09-19

### Added

- **Death markers can be cleared.** Right click one to remove it, the same as any other pin.

## [1.0.2] - 2026-09-18

### Changed

- **The pin legend fits the screen.** It now hangs from the top right corner and sizes itself to the
  height of the map, so it no longer runs off the bottom. **Legend Scale** is gone with it: there is
  nothing left to set.

- **The map is tidier.** The cartography table and public position toggles sit in the bottom left
  corner, and the key hints are a list in the top left instead of a row along the bottom.

## [1.0.1] - 2026-09-17

### Changed

- **The pin legend is smaller.** With every BetterMap icon added to it the column ran most of the
  height of the screen. The whole legend now draws at three quarters size, the game's own rows and
  ours together, and **Legend Scale** under Map adjusts it.


## [1.0.0] - 2026-09-10

First release, built for Valheim 1.0.

### Added

- **Creatures on the map**, drawn as their own trophy. Creatures with no trophy get a plain pin
  with their name, so you always know what is there. Tames are tinted green and show a name if you
  gave them one; anything that attacks on sight is tinted red.

- **Boats and carts.** The ones you built or have driven show up wherever you left them, however
  far away. Boats point the way they are facing. Anyone else's only shows when you are near it.

- **Resources pinned as you find them**, with a checkbox for every one, grouped by biome. Ore,
  berries, seeds, tar pits, beehives, crypts, caves, vegvisirs and more. A pin you delete stays
  deleted.

- **Death markers that survive a reload.** The game drops one where you died but never saves it.
  BetterMap keeps them, and keeps more than one.

- **Trader names.** Haldor, Hildir and the bog witch get their names on the map. There is also a
  setting, off by default, that puts every trader in the world on your map at once.

- **Portals**, pinned where you place them and named by their tag. Rename a portal and its pin
  follows.

- **New icons in the map legend**, one per kind of pin, so you can place them by hand and hide them
  with a right click like the game's own.

- **Configurable exploration radius and pin size.**

- **ServerSync**, so a server decides the settings for everyone on it.
