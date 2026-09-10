# BetterMap — pin curation
What the mod pins, decided biome by biome against the game's own data rather than a wiki.
Generated from the three dumps: 4597 prefabs, 257 vegetation entries, 232 locations, Valheim 1.0.7.

## Rules
- Config is **sectioned by biome**, with a checkbox for **every single resource**. No grouped lumps.
- The same resource can be on in one biome and off in another, so the pin decision needs the
  biome at the object's position (`Heightmap.FindBiome`), not just the prefab name.
- **Dungeon interiors are never pinned.**
- **Boss altars are off by default.** Vanilla already pins them when you use a vegvisir,
  so pinning them automatically duplicates a mechanic the game already has.
- At the end of the curation the off-by-default lists get a pruning pass: anything that is
  simply not worth a checkbox is dropped entirely rather than shipped as an unused config entry.
- **Anything vanilla already pins is out of scope**, with no config entry at all: traders,
  Hildir's camps, the bog witch, the start temple, the Deep North boss room. Those are the
  locations flagged `icon:always` or `icon:placed` in the dump.
- Pins are detected from objects loaded in the world. The placement tables below are the menu
  for deciding what to pin, not a runtime lookup.
- Auto pins never remove themselves, so a new pin is skipped when one already exists within
  the merge distance.

---

## Meadows
### Decided

**On by default:** `Beehive`

**Off by default:** `Eikthyrnir` (boss altar) · `RaspberryBush` · `Pickable_Flint` · `Pickable_Mushroom` · `Pickable_Dandelion` · `Runestone_Boars`

`Runestone_Boars` carries nine `Spawner_Boar` creature spawners, so it is a renewable boar farm rather than a lore stone. `Runestone_Meadows` has no spawner and is flavour only.

Beehives are in `WoodHouse1,2,3,4,5,6,7,9,10,11,13` (qty 20 each), `BearCave` (50) and `StoneTowerRuins03` (80), one per building, and are the only source of QueenBee.

### Scattered (vegetation)

| prefab | qty per zone | yields |
|---|---|---|
| `Pickable_Dandelion` | 3-5, 40-60, 8-10 | Dandelion x1 |
| `Pickable_Flint` | 30-30 | Flint x1 |
| `Pickable_Mushroom` | 1-2, 3-5, 80-100 | Mushroom x1 |
| `RaspberryBush` | 1-2, 2-3 | Raspberry x1 |

### Locations

| location | qty | contents |
|---|---|---|
| `CombatRuin01` | 5 | Container ×1<br>SpawnArea ×1 |
| `Dolmen01` | 100 | CreatureSpawner ×1 |
| `Dolmen02` | 100 | CreatureSpawner ×1 |
| `Dolmen03` | 50 | CreatureSpawner ×1 |
| `Eikthyrnir` | 3 | OfferingBowl ×1<br>RuneStone ×1 |
| `Hildir_camp` *icon:placed  unique* | 10 | Vegvisir ×1 |
| `Runestone_Boars` | 50 | CreatureSpawner ×9<br>RuneStone ×1 |
| `Runestone_Meadows` | 100 | RuneStone ×1 |
| `ShipSetting01` | 100 | Container ×1<br>CreatureSpawner ×1 |
| `StartTemple` *icon:always* | 1 | `RaspberryBush` → Raspberry x1<br>`Pickable_Mushroom` → Mushroom x1<br>RuneStone ×5<br>Vegvisir ×1 |
| `WoodHouse1` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1<br>Container ×1 |
| `WoodHouse10` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1<br>Container ×1 |
| `WoodHouse11` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1<br>CreatureSpawner ×1<br>Container ×1 |
| `WoodHouse12` | 20 | `Pickable_Mushroom` → Mushroom x1<br>Container ×1 |
| `WoodHouse13` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1<br>CreatureSpawner ×1<br>Container ×1 |
| `WoodHouse2` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1<br>Container ×1 |
| `WoodHouse3` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1 |
| `WoodHouse4` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1 |
| `WoodHouse5` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1 |
| `WoodHouse6` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1<br>CreatureSpawner ×2<br>Container ×1 |
| `WoodHouse7` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1<br>Container ×1 |
| `WoodHouse8` | 20 | `Pickable_Dandelion` → Dandelion x1 |
| `WoodHouse9` | 20 | `Beehive` → Honey x1-3, QueenBee x1-1<br>Container ×1 |

---

## BlackForest

### Decided

**On by default:** `rock4_copper` (CopperOre) · `MineRock_Tin` (TinOre) · `Beehive` ·
`BlueberryBush` · `Pickable_Thistle` · `Pickable_SeedCarrot` · `BearCave` · `TrollCave02` ·
`Crypt2` `Crypt3` `Crypt4`

**Off by default:** `GDKing` (boss altar) · `Pickable_Mushroom` · `Runestone_Greydwarfs` ·
`Runestone_BlackForest` · `ShipWreck01-04` · `Ruin1` · `Ruin2` · `StoneHouse3` · `StoneHouse4` ·
`StoneTowerRuins03/07/08/09/10` and the sunk variants · `Greydwarf_camp1` · `Dolmen01-03` ·
`BigRockClearing`

Crypt entrances are pinned. The no dungeon rule covers what is inside them, not the way in.

`StoneTowerRuins03` is off, but the beehive inside it is still pinned: pins are placed on the
object, not on the location that happens to contain it.

### Scattered (vegetation)

| prefab | qty per zone | yields |
|---|---|---|
| `BlueberryBush` | 1-1, 3-5 | Blueberries x1 |
| `MineRock_Tin` | 20-20 | TinOre x1-1 |
| `Pickable_Mushroom` | 1-2, 3-5, 80-100 | Mushroom x1 |
| `Pickable_SeedCarrot` | 0-0,5 | CarrotSeeds x3 |
| `Pickable_Thistle` | 1-2 | Thistle x1 |
| `rock4_copper` | 0-1 | Stone x1-1, CopperOre x1-1 |

### Locations

| location | qty | contents |
|---|---|---|
| `BearCave` | 50 | `Pickable_Mushroom_yellow` → MushroomYellow x1<br>`Beehive` → Honey x1-3, QueenBee x1-1<br>`BlueberryBush` → Blueberries x1<br>CreatureSpawner ×1 |
| `BigRockClearing` *unique* | 10 | `Pickable_Thistle` → Thistle x1 |
| `Crypt2` | 200 | CreatureSpawner ×3 |
| `Crypt3` | 200 | CreatureSpawner ×3 |
| `Crypt4` | 200 | CreatureSpawner ×3 |
| `Dolmen01` | 100 | CreatureSpawner ×1 |
| `Dolmen02` | 100 | CreatureSpawner ×1 |
| `Dolmen03` | 50 | CreatureSpawner ×1 |
| `GDKing` | 4 | OfferingBowl ×1<br>RuneStone ×1 |
| `Greydwarf_camp1` | 300 | `Spawner_GreydwarfNest` → AncientSeed x1-1<br>SpawnArea ×1 |
| `Hildir_crypt` *icon:placed* | 3 | CreatureSpawner ×3 |
| `Ruin1` | 200 | CreatureSpawner ×6<br>Container ×1 |
| `Ruin2` | 200 | `barrell` → Blueberries x2-4, DeerHide x2-3, Flint x<br>CreatureSpawner ×8<br>Container ×1<br>Vegvisir ×1 |
| `Runestone_BlackForest` | 50 | RuneStone ×1 |
| `Runestone_Greydwarfs` | 25 | CreatureSpawner ×5<br>RuneStone ×1 |
| `ShipWreck01` | 25 | Container ×1 |
| `ShipWreck02` | 25 | Container ×1 |
| `ShipWreck03` | 25 | Container ×1 |
| `ShipWreck04` | 25 | Container ×1 |
| `StoneHouse3` | 200 | CreatureSpawner ×1<br>Container ×1 |
| `StoneHouse4` | 200 | CreatureSpawner ×2 |
| `StoneTowerRuins03` | 80 | `Beehive` → Honey x1-3, QueenBee x1-1<br>CreatureSpawner ×11<br>Container ×2<br>Vegvisir ×1 |
| `StoneTowerRuins07` | 80 | CreatureSpawner ×6<br>Container ×1 |
| `StoneTowerRuins08` | 80 | CreatureSpawner ×6<br>Container ×1 |
| `StoneTowerRuins09` | 80 | CreatureSpawner ×7<br>Container ×1 |
| `StoneTowerRuins09_sunk` | 10 | CreatureSpawner ×2<br>Container ×1 |
| `StoneTowerRuins10` | 80 | CreatureSpawner ×7<br>Container ×1 |
| `StoneTowerRuins10_sunk` | 10 | Container ×1<br>CreatureSpawner ×1 |
| `TrollCave02` | 200 | `Pickable_Mushroom_yellow` → MushroomYellow x1<br>Container ×4<br>CreatureSpawner ×3 |

---

## Swamp

### Decided

**On by default:** `Pickable_SeedTurnip` · `Pickable_Thistle` · `SunkenCrypt4` · `FireHole` ·
`Runestone_Draugr`

**Off by default:** `Bonemass` (boss altar) · `mudpile_beacon` · `Pickable_Mushroom` ·
`InfestedTree01` · `Grave1` · `SwampHut1-5` and variants · `SwampRuin1/2` · `SwampWell1` ·
`ShipWreck01-04` · `Runestone_Swamps`

**Out of scope:** `BogWitch_Camp`, vanilla pins it.

`FireHole` is in because surtling cores are gated behind it and there are only 75 per world.
`Runestone_Draugr` carries three spawners, the same farm pattern as the boars and greydwarfs.

### Scattered (vegetation)

| prefab | qty per zone | yields |
|---|---|---|
| `Pickable_Mushroom` | 1-2 | Mushroom x1 |
| `Pickable_SeedTurnip` | 0-0,5 | TurnipSeeds x3 |
| `Pickable_Thistle` | 1-2 | Thistle x1 |
| `mudpile_beacon` | 0-5 | IronScrap x1-1, WitheredBone x1-1 |

### Locations

| location | qty | contents |
|---|---|---|
| `BogWitch_Camp` *icon:placed  unique* | 10 | SpawnArea ×1 |
| `Bonemass` | 5 | OfferingBowl ×1<br>RuneStone ×1 |
| `FireHole` | 75 | CreatureSpawner ×3 |
| `Grave1` | 200 | SpawnArea ×3<br>CreatureSpawner ×3<br>Container ×1 |
| `InfestedTree01` | 700 | `GuckSack` → Guck x1-1<br>`GuckSack_small` → Guck x1-1 |
| `Runestone_Draugr` | 50 | CreatureSpawner ×3<br>RuneStone ×1 |
| `Runestone_Swamps` | 100 | RuneStone ×1 |
| `ShipWreck01` | 25 | Container ×1 |
| `ShipWreck02` | 25 | Container ×1 |
| `ShipWreck03` | 25 | Container ×1 |
| `ShipWreck04` | 25 | Container ×1 |
| `SunkenCrypt4` | 175 | CreatureSpawner ×2 |
| `SwampHut1` | 50 | Container ×1<br>CreatureSpawner ×1 |
| `SwampHut1_1` | 50 | Container ×1<br>CreatureSpawner ×1 |
| `SwampHut2` | 50 | Container ×1<br>CreatureSpawner ×1 |
| `SwampHut2_1` | 50 | Container ×1<br>CreatureSpawner ×1 |
| `SwampHut3` | 50 | Container ×1<br>CreatureSpawner ×1 |
| `SwampHut3_1` | 50 | Container ×1<br>CreatureSpawner ×1 |
| `SwampHut4` | 50 | CreatureSpawner ×4<br>Container ×1 |
| `SwampHut5` | 25 | Container ×1<br>CreatureSpawner ×1 |
| `SwampRuin1` | 30 | CreatureSpawner ×3<br>Vegvisir ×1<br>Container ×1<br>SpawnArea ×1 |
| `SwampRuin2` | 30 | CreatureSpawner ×3<br>Vegvisir ×1<br>Container ×1<br>SpawnArea ×1 |
| `SwampWell1` | 25 | CreatureSpawner ×2 |

---

## Mountain

### Decided

**On by default:** `silvervein` (SilverOre) · `MineRock_Obsidian` · `DrakeNest01` (DragonEgg)

**Off by default:** `Dragonqueen` (boss altar) · `AbandonedLogCabin02/03/04` ·
`StoneTowerRuins04/05` and `_leet` · `MountainWell1` · `DrakeLorestone` · `Runestone_Mountains`

**Out of scope:** `AncientUpgradeStation`, `Hildir_cave`, vanilla pins both.

`silvervein` is one per zone and normally needs a wishbone to find, which makes it the most
useful pin in the biome.

### Scattered (vegetation)

| prefab | qty per zone | yields |
|---|---|---|
| `MineRock_Obsidian` | 10-15 | Obsidian x1-1 |
| `silvervein` | 1-1 | Stone x1-1, SilverOre x1-1 |

### Locations

| location | qty | contents |
|---|---|---|
| `AbandonedLogCabin02` | 33 | Container ×2<br>CreatureSpawner ×2 |
| `AbandonedLogCabin03` | 33 | CreatureSpawner ×3<br>Container ×1 |
| `AbandonedLogCabin04` | 50 | CreatureSpawner ×4<br>Container ×1 |
| `AncientUpgradeStation` *icon:placed  unique* | 10 | RuneStone ×1 |
| `Dragonqueen` | 3 | RuneStone ×1<br>OfferingBowl ×1 |
| `DrakeLorestone` | 50 | RuneStone ×1 |
| `DrakeNest01` | 200 | `Pickable_DragonEgg` → DragonEgg x1<br>CreatureSpawner ×3 |
| `Hildir_cave` *icon:placed* | 3 | `MountainKit_brazier_blue` → Bronze x1-1, Coal x1-2, Coal x1-1 |
| `MountainWell1` | 25 | Container ×1 |
| `Runestone_Mountains` | 100 | RuneStone ×1 |
| `StoneTowerRuins04` | 50 | CreatureSpawner ×5<br>Container ×2<br>Vegvisir ×1 |
| `StoneTowerRuins05` | 50 | CreatureSpawner ×19<br>Container ×1<br>SpawnArea ×1 |
| `StoneTowerRuins05_leet` | 10 | CreatureSpawner ×19<br>Container ×1<br>SpawnArea ×1 |

---

## Plains

### Decided

**On by default:** `TarPit1` `TarPit1_1` `TarPit2` `TarPit2_1` `TarPit3` `TarPit3_1` ·
`CloudberryBush`

**Off by default:** `GoblinKing` (boss altar) · `GoblinHut01/02/03` · `StoneTower1/3` ·
`StoneHenge1-5` · `Ruin3` · `ShipWreck01-04` · `Runestone_Plains`

**Dropped:** `StoneHouse1_heath`, `StoneHouse2_heath`, `StoneHouse5_heath` have a quantity of
zero and never generate, so they get no entry at all.

Tar has no other source, which is what earns the pits their pin despite there being 386 of them.

### Scattered (vegetation)

| prefab | qty per zone | yields |
|---|---|---|
| `CloudberryBush` | 1-3 | Cloudberry x1 |

### Locations

| location | qty | contents |
|---|---|---|
| `GoblinHut01` | 30 | `goblin_roof_45d_corner` → DeerHide x1-1<br>CreatureSpawner ×3 |
| `GoblinHut02` | 30 | `goblin_roof_45d` → DeerHide x1-1<br>`goblin_roof_45d_corner` → DeerHide x1-1<br>`goblin_roof_cap` → DeerHide x1-1<br>CreatureSpawner ×5<br>Container ×1 |
| `GoblinHut03` | 20 | `goblin_banner` → Wood x1-1, DeerHide x1-1<br>`Pickable_Dandelion` → Dandelion x1<br>`goblin_roof_45d_corner` → DeerHide x1-1<br>CreatureSpawner ×8<br>Container ×1 |
| `GoblinKing` | 4 | OfferingBowl ×1<br>RuneStone ×1 |
| `Ruin3` | 50 | CreatureSpawner ×2<br>Container ×1 |
| `Runestone_Plains` | 100 | RuneStone ×1 |
| `ShipWreck01` | 25 | Container ×1 |
| `ShipWreck02` | 25 | Container ×1 |
| `ShipWreck03` | 25 | Container ×1 |
| `ShipWreck04` | 25 | Container ×1 |
| `StoneHenge1` | 5 | CreatureSpawner ×3<br>Container ×1<br>Vegvisir ×1 |
| `StoneHenge2` | 5 | CreatureSpawner ×3<br>Container ×1 |
| `StoneHenge3` | 5 | CreatureSpawner ×3<br>Container ×1<br>Vegvisir ×1 |
| `StoneHenge4` | 5 | CreatureSpawner ×2<br>Vegvisir ×1 |
| `StoneHenge5` | 20 | CreatureSpawner ×3<br>Vegvisir ×1 |
| `StoneHouse1_heath` | 0 | CreatureSpawner ×2<br>Container ×1 |
| `StoneHouse2_heath` | 0 | CreatureSpawner ×2 |
| `StoneHouse5_heath` | 0 | Container ×1 |
| `StoneTower1` | 50 | `goblin_banner` → Wood x1-1, DeerHide x1-1<br>`goblin_roof_45d_corner` → DeerHide x1-1<br>`goblin_roof_45d` → DeerHide x1-1<br>CreatureSpawner ×9<br>Container ×1<br>Vegvisir ×1 |
| `StoneTower3` | 50 | `goblin_banner` → Wood x1-1, DeerHide x1-1<br>`goblin_roof_45d` → DeerHide x1-1<br>`goblin_roof_45d_corner` → DeerHide x1-1<br>CreatureSpawner ×12<br>Container ×1<br>Vegvisir ×1 |
| `TarPit1` | 100 | `Pickable_TarBig` → Tar x15<br>`Pickable_Tar` → Tar x4<br>CreatureSpawner ×9 |
| `TarPit1_1` | 50 | `Pickable_TarBig` → Tar x15<br>`Pickable_Tar` → Tar x4<br>CreatureSpawner ×9 |
| `TarPit2` | 16 | `Pickable_Tar` → Tar x4<br>`Pickable_TarBig` → Tar x15<br>CreatureSpawner ×9 |
| `TarPit2_1` | 20 | `Pickable_Tar` → Tar x4<br>`Pickable_TarBig` → Tar x15<br>CreatureSpawner ×9 |
| `TarPit3` | 100 | `Pickable_TarBig` → Tar x15<br>`Pickable_Tar` → Tar x4<br>CreatureSpawner ×6 |
| `TarPit3_1` | 100 | `Pickable_TarBig` → Tar x15<br>`Pickable_Tar` → Tar x4<br>CreatureSpawner ×6 |

---

## Mistlands

### Decided

**On by default:** `YggdrasilRoot` (sap) · `giant_ribs` (BlackMarble) · `giant_helmet1` ·
`giant_helmet2` · `giant_sword1` · `giant_sword2` · `Pickable_Mushroom_Magecap` ·
`Pickable_Mushroom_JotunPuffs` · `Mistlands_DvergrBossEntrance1` ·
`Mistlands_Excavation1/2/3`

**Off by default:** `YggaShoot_small1` · `Mistlands_DvergrTownEntrance1/2` · `Mistlands_RoadPost1` ·
`Mistlands_RockSpire1` · `Mistlands_Giant1` · `Mistlands_GuardTower1/2/3` and the ruined variants ·
`Mistlands_Harbour1` · `Mistlands_Lighthouse1_new` · `Runestone_Mistlands`

The Queen is behind `DvergrBossEntrance1`, a location rather than an offering bowl, so the boss
altar rule does not apply to it. Softtissue is gated behind the excavations the same way surtling
cores are gated behind fire holes.

### Scattered (vegetation)

| prefab | qty per zone | yields |
|---|---|---|
| `Pickable_Mushroom_JotunPuffs` | 1-2 | MushroomJotunPuffs x1 |
| `Pickable_Mushroom_Magecap` | 2-2 | MushroomMagecap x1 |
| `YggaShoot_small1` | 100-100, 40-40, 6-6, 60-60 | Wood x1-1, YggdrasilWood x1-1 |
| `YggdrasilRoot` | 1-3, 2-4 | sap |
| `giant_helmet1` | 1-2, 3-3 | IronScrap x1-1, CopperScrap x1-1 |
| `giant_helmet2` | 1-2, 3-3 | IronScrap x1-1, CopperScrap x1-1 |
| `giant_ribs` | 1-2 | BlackMarble x1-1 |
| `giant_sword1` | 1-2, 3-5, 3-5 | IronScrap x1-1, CopperScrap x1-1 |
| `giant_sword2` | 1-2, 3-5, 3-5 | IronScrap x1-1, CopperScrap x1-1 |

### Locations

| location | qty | contents |
|---|---|---|
| `Mistlands_DvergrBossEntrance1` | 5 | `dvergrprops_banner` → JuteBlue x1-1<br>`dvergrprops_curtain` → JuteBlue x1-1<br>`Pickable_DvergrStein` → Tankard_dvergr x1<br>CreatureSpawner ×4<br>RuneStone ×1 |
| `Mistlands_DvergrTownEntrance1` | 120 | `blackmarble_post01` → BlackMarble x1-1<br>CreatureSpawner ×5 |
| `Mistlands_DvergrTownEntrance2` | 120 | `dvergrprops_banner` → JuteBlue x1-1<br>`dvergrprops_curtain` → JuteBlue x1-1<br>CreatureSpawner ×4 |
| `Mistlands_Excavation1` | 40 | `dvergrprops_wood_wall` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_crate` → FineWood x1-1, Softtissue x2-4<br>CreatureSpawner ×12 |
| `Mistlands_Excavation2` | 40 | `dvergrprops_wood_wall` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_crate` → FineWood x1-1, Softtissue x2-4<br>`dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>CreatureSpawner ×12 |
| `Mistlands_Excavation3` | 40 | `dvergrprops_wood_wall` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_crate` → FineWood x1-1, Softtissue x2-4<br>`dvergrtown_wood_crane` → Wood x1-1, CopperScrap x1-1, Chain x1-1<br>CreatureSpawner ×6 |
| `Mistlands_Giant1` | 250 | CreatureSpawner ×5 |
| `Mistlands_GuardTower1_new` | 75 | `dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_curtain` → JuteBlue x1-1<br>`dvergrprops_banner` → JuteBlue x1-1<br>CreatureSpawner ×8 |
| `Mistlands_GuardTower1_ruined_new` | 80 | `dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_curtain` → JuteBlue x1-1<br>`dvergrprops_banner` → JuteBlue x1-1<br>CreatureSpawner ×7<br>Container ×1 |
| `Mistlands_GuardTower1_ruined_new2` | 20 | `dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_curtain` → JuteBlue x1-1<br>`dvergrprops_banner` → JuteBlue x1-1<br>CreatureSpawner ×3<br>Container ×1 |
| `Mistlands_GuardTower2_new` | 75 | `dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_curtain` → JuteBlue x1-1<br>`dvergrprops_banner` → JuteBlue x1-1<br>CreatureSpawner ×8 |
| `Mistlands_GuardTower3_new` | 50 | `dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_curtain` → JuteBlue x1-1<br>`dvergrprops_crate` → FineWood x1-1, Softtissue x2-4<br>CreatureSpawner ×7 |
| `Mistlands_GuardTower3_ruined_new` | 50 | `dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_curtain` → JuteBlue x1-1<br>`blackmarble_post01` → BlackMarble x1-1<br>CreatureSpawner ×4<br>Container ×1 |
| `Mistlands_Harbour1` | 100 | `dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_crate` → FineWood x1-1, Softtissue x2-4<br>`dvergrtown_wood_crane` → Wood x1-1, CopperScrap x1-1, Chain x1-1<br>CreatureSpawner ×5 |
| `Mistlands_Lighthouse1_new` | 100 | `dvergrprops_curtain` → JuteBlue x1-1<br>`dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>`dvergrprops_banner` → JuteBlue x1-1<br>CreatureSpawner ×6 |
| `Mistlands_RoadPost1` | 500 | `blackmarble_post01` → BlackMarble x1-1<br>CreatureSpawner ×2 |
| `Mistlands_RockSpire1` | 200 | `blackmarble_post01` → BlackMarble x1-1<br>Container ×1<br>CreatureSpawner ×1 |
| `Runestone_Mistlands` | 50 | RuneStone ×1 |

---

## AshLands

### Decided

**On by default:** `UnstableLavaRock` (ProustitePowder) · `Pickable_SmokePuff` ·
`ashland_pot2_red` · `LeviathanLava` (FlametalOreNew) · `PlaceofMystery1/2/3` ·
`CharredFortress` · `SulfurArch`

**Off by default:** `FaderLocation` (boss altar) · `Pickable_Charredskull` · `MorgenHole1/2/3` ·
`VoltureNest` · `CharredTowerRuins1` and `_dvergr` · `CharredTowerRuins3` · `CharredRuins1-4` ·
`CharredStone_Spawner` · `Runestone_Ashlands`

`PlaceofMystery1/2/3` are one of each per world and hold the three Dyrnwyn fragments, which makes
them the scarcest thing in the game.

### Scattered (vegetation)

| prefab | qty per zone | yields |
|---|---|---|
| `Pickable_Charredskull` | 20-40 | Charredskull x1 |
| `Pickable_SmokePuff` | 2-2 | MushroomSmokePuff x1 |
| `UnstableLavaRock` | 1-1 | ProustitePowder x1-1 |
| `ashland_pot2_red` | 1-2 | Pot_Shard_Green x1-1, Pot_Shard_Green x1-1, Bronze x1-1, Iro |

### Locations

| location | qty | contents |
|---|---|---|
| `CharredFortress` | 20 | `Ashlands_Fortress_Wall_Spikes` → BronzeScrap x1-1<br>`Charred_altar_bellfragment` → BellFragment x1-1<br>`Pickable_MoltenCoreStand` → MoltenCore x1<br>CreatureSpawner ×12<br>Container ×4<br>SpawnArea ×2<br>Vegvisir ×1 |
| `CharredRuins1` | 75 | `ashland_pot3_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>`ashland_pot2_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>`ashland_pot1_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>Vegvisir ×2 |
| `CharredRuins2` | 100 | `ashland_pot3_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>`ashland_pot2_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>`ashland_pot1_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>Vegvisir ×1 |
| `CharredRuins3` | 100 | `ashland_pot3_green` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>`ashland_pot2_green` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>`ashland_pot1_green` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>Vegvisir ×1 |
| `CharredRuins4` | 100 | `ashland_pot3_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>`ashland_pot2_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>`ashland_pot1_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>Vegvisir ×1 |
| `CharredStone_Spawner` | 300 | `Spawner_CharredStone` → Grausten x1-1, Charredskull x1-1<br>SpawnArea ×1 |
| `CharredTowerRuins1` | 30 | `Pickable_Fiddlehead` → Fiddleheadfern x1<br>`ashland_pot1_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1<br>`ashland_pot2_red` → Pot_Shard_Green x1-1, Pot_Shard_Green x1 |
| `CharredTowerRuins1_dvergr` | 30 | `Pickable_Fiddlehead` → Fiddleheadfern x1<br>`dvergrprops_wood_pole` → Wood x1-1, CopperScrap x1-1<br>`Pickable_SmokePuff` → MushroomSmokePuff x1<br>CreatureSpawner ×5 |
| `CharredTowerRuins3` | 30 | `Spawner_CharredStone` → Grausten x1-1, Charredskull x1-1<br>SpawnArea ×1 |
| `FaderLocation` | 3 | RuneStone ×1<br>OfferingBowl ×1 |
| `LeviathanLava` | 100 | `LeviathanLava` → FlametalOreNew x1-1  (tier 3, hp 100) |
| `MorgenHole1` | 40 | `asksvin_carrion` → BoneFragments x2-15, AsksvinCarrionSkull<br>`asksvin_carrion2` → BoneFragments x2-15, AsksvinCarrionSkull<br>`Pickable_MeatPile` → Entrails x1<br>Container ×3<br>CreatureSpawner ×1<br>Vegvisir ×1 |
| `MorgenHole2` | 40 | `asksvin_carrion` → BoneFragments x2-15, AsksvinCarrionSkull<br>`asksvin_carrion2` → BoneFragments x2-15, AsksvinCarrionSkull<br>`Pickable_MeatPile` → Entrails x1<br>Container ×3<br>CreatureSpawner ×1<br>Vegvisir ×1 |
| `MorgenHole3` | 40 | `asksvin_carrion` → BoneFragments x2-15, AsksvinCarrionSkull<br>`asksvin_carrion2` → BoneFragments x2-15, AsksvinCarrionSkull<br>`Pickable_MeatPile` → Entrails x1<br>Container ×3<br>CreatureSpawner ×1<br>Vegvisir ×1 |
| `PlaceofMystery1` *unique* | 1 | `Pickable_Swordpiece3` → DyrnwynTipFragment x1<br>`Spawner_CharredStone_Elite` → Grausten x1-1, Charredskull x1-1<br>`Pickable_MoltenCoreStand` → MoltenCore x1<br>SpawnArea ×1<br>Vegvisir ×1 |
| `PlaceofMystery2` *unique* | 1 | `Pickable_Swordpiece2` → DyrnwynBladeFragment x1<br>`Spawner_CharredStone_Elite` → Grausten x1-1, Charredskull x1-1<br>`Pickable_MoltenCoreStand` → MoltenCore x1<br>SpawnArea ×1<br>Vegvisir ×1 |
| `PlaceofMystery3` *unique* | 1 | `Pickable_MoltenCoreStand` → MoltenCore x1<br>`Spawner_CharredStone_Elite` → Grausten x1-1, Charredskull x1-1<br>CreatureSpawner ×1<br>SpawnArea ×1 |
| `Runestone_Ashlands` | 70 | RuneStone ×1 |
| `SulfurArch` | 100 | `Pickable_SulfurRock` → SulfurStone x1 |
| `VoltureNest` | 350 | `asksvin_carrion` → BoneFragments x2-15, AsksvinCarrionSkull<br>`asksvin_carrion2` → BoneFragments x2-15, AsksvinCarrionSkull<br>`Pickable_VoltureEgg` → VoltureEgg x1<br>CreatureSpawner ×4 |

---

## DeepNorth

### Decided

**On by default:** `DN_gammeltrollFrac01` `DN_gammeltrollFrac02` (GoldOre) · `MorkBorg` ·
`NorthMemorialPlace` · `Pickable_SeedKale` · `LingonberryBush`

**Off by default:** `Pickable_Snowball` · `TheHole01` · `ShipWreck01_DN` · `ShipWreck02_DN` ·
`ShipSetting02` · `ShipSetting03` · `DN_hut01` · `LumberCamp` · `Runestone_DeepNorth`

**Dropped:** `ice_rock1` yields nothing.

**Out of scope:** `DN_Bossroom`, vanilla pins it.

The frozen trolls are the only source of gold. `NorthMemorialPlace` holds eleven containers at
fifteen per world, the densest loot site anywhere in the dump.

### Scattered (vegetation)

| prefab | qty per zone | yields |
|---|---|---|
| `LingonberryBush` | 1-2 | Lingonberry x1 |
| `Pickable_SeedKale` | 0-0,5 | KaleSeeds x3 |
| `Pickable_Snowball` | 2-5 | Snowball x1 |
| `ice_rock1` | 0-1, 0-1 | (nothing) |

### Locations

| location | qty | contents |
|---|---|---|
| `DN_Bossroom` *icon:placed* | 3 | OfferingBowl ×2<br>RuneStone ×1 |
| `DN_gammeltrollFrac01` | 30 | `TrollFrost_Frac_legs` → Stone x1-1, GoldOre x1-1  (tier 6, hp 50 |
| `DN_gammeltrollFrac02` | 30 | `TrollFrost_Frac_arm` → Stone x1-1, GoldOre x1-1  (tier 6, hp 50 |
| `DN_hut01` | 40 | Container ×1 |
| `LumberCamp` | 50 | Container ×1 |
| `MorkBorg` | 40 | `Morkhalla_Eye1` → AncientGemstoneBlack x1<br>`Morkhalla_Eye2` → AncientGemstoneGreen x1<br>`Morkhalla_Eye3` → AncientGemstoneOrange x1<br>Container ×2<br>SpawnArea ×1 |
| `NorthMemorialPlace` | 15 | Container ×11<br>RuneStone ×1<br>Vegvisir ×1<br>OfferingBowl ×1 |
| `Runestone_DeepNorth` | 70 | RuneStone ×1 |
| `ShipSetting02` | 100 | Container ×1 |
| `ShipSetting03` | 50 | Container ×3 |
| `ShipWreck01_DN` | 170 | Container ×1 |
| `ShipWreck02_DN` | 120 | Container ×1 |
| `TheHole01` | 40 | `prop_cauldron_ext3_butchertable` → FineWood x1-5, RoundLog x1-5<br>`prop_piece_MeadCauldron` → BronzeScrap x1-5<br>`prop_piece_cauldron` → TinOre x1-5<br>Container ×5<br>CreatureSpawner ×4 |

---

## Ocean

### Scattered (vegetation)

| prefab | qty per zone | yields |
|---|---|---|
| `Leviathan` | 0-0,01 | Chitin x1-1 |

### Locations

| location | qty | contents |
|---|---|---|
| `ShipWreck01` | 25 | Container ×1 |
| `ShipWreck02` | 25 | Container ×1 |
| `ShipWreck03` | 25 | Container ×1 |
| `ShipWreck04` | 25 | Container ×1 |
