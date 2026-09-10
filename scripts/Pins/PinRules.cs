using System.Collections.Generic;
using BepInEx.Configuration;

namespace BetterMap.Scripts.Pins
{
    public enum PinCategory
    {
        Ore,
        Forage,
        Dungeon,
        Loot,
        Spawner,
        Vegvisir,
        Beehive,
        Tar,
        Sap,
        Boss,
        Portal
    }

    /// <summary>
    /// What can be pinned. Curated by hand and written up in PINS.md, which holds the reasoning for
    /// every entry; this table is generated from that file so the two cannot drift.
    ///
    /// One row is one setting, in the player's terms rather than the game's. Six tar pit prefabs are
    /// one box called Tar Pits, because that is one thing as far as anyone playing is concerned.
    ///
    /// The same thing can be worth pinning in one biome and not in another, so a row is keyed by
    /// both, and the biome comes from where the object stands rather than from its name.
    /// </summary>
    public static class PinRules
    {
        public class Rule
        {
            public string Name;
            public string Description;

            /// <summary>
            /// What a pin made from this is called, when the object itself carries no name worth
            /// using. Null means the name is read off the object.
            /// </summary>
            public string NameToken;
            public string[] Prefabs;
            public Heightmap.Biome Biome;
            public PinCategory Category;
            public bool DefaultOn;
            public bool IsLocation;

            /// <summary>Bound at startup, one checkbox per row.</summary>
            public ConfigEntry<bool> Enabled;
        }

        public static readonly List<Rule> All = new List<Rule>
        {
            new Rule
            {
                Name = "Beehives",
                Description = "Wild beehives, in and around the abandoned houses. Honey and a queen bee.",
                NameToken = "$piece_beehive",
                Prefabs = new[] { "Beehive" },
                Biome = Heightmap.Biome.Meadows,
                Category = PinCategory.Beehive,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Vegvisirs",
                Description = "The runestones that reveal a boss on your map.",
                NameToken = "$piece_vegvisir",
                Prefabs = new[] { "Vegvisir_Eikthyr", "Vegvisir_GDKing", "Vegvisir_Bonemass", "Vegvisir_DragonQueen", "Vegvisir_GoblinKing", "Vegvisir_Fader", "Vegvisir_DNBoss" },
                Biome = Heightmap.Biome.Meadows,
                Category = PinCategory.Vegvisir,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Raspberry Bushes",
                Description = "Raspberry bushes.",
                Prefabs = new[] { "RaspberryBush" },
                Biome = Heightmap.Biome.Meadows,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Flint",
                Description = "Flint along the shoreline.",
                Prefabs = new[] { "Pickable_Flint" },
                Biome = Heightmap.Biome.Meadows,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Mushrooms",
                Description = "Ordinary mushrooms.",
                Prefabs = new[] { "Pickable_Mushroom" },
                Biome = Heightmap.Biome.Meadows,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Dandelions",
                Description = "Dandelions.",
                Prefabs = new[] { "Pickable_Dandelion" },
                Biome = Heightmap.Biome.Meadows,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Boar Runestones",
                Description = "Runestones ringed by boar spawners. A place to farm, not a place to gather.",
                Prefabs = new[] { "Runestone_Boars" },
                Biome = Heightmap.Biome.Meadows,
                Category = PinCategory.Spawner,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Altar of Eikthyr",
                Description = "Where Eikthyr is summoned. Off by default: use a vegvisir and the game pins it for you.",
                Prefabs = new[] { "Eikthyrnir" },
                Biome = Heightmap.Biome.Meadows,
                Category = PinCategory.Boss,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Vegvisirs",
                Description = "The runestones that reveal a boss on your map. Pinned wherever they stand, rather than the ruin around them.",
                NameToken = "$piece_vegvisir",
                Prefabs = new[] { "Vegvisir_Eikthyr", "Vegvisir_GDKing", "Vegvisir_Bonemass", "Vegvisir_DragonQueen", "Vegvisir_GoblinKing", "Vegvisir_Fader", "Vegvisir_DNBoss" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Vegvisir,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Copper Deposits",
                Description = "The large half buried rocks you mine for copper.",
                Prefabs = new[] { "rock4_copper" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Ore,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Tin Deposits",
                Description = "Tin along the water's edge.",
                Prefabs = new[] { "MineRock_Tin" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Ore,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Beehives",
                Description = "Wild beehives in the abandoned houses.",
                NameToken = "$piece_beehive",
                Prefabs = new[] { "Beehive" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Beehive,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Blueberry Bushes",
                Description = "Blueberry bushes. Common enough that pinning them fills the map quickly.",
                Prefabs = new[] { "BlueberryBush" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Thistle",
                Description = "Thistle, which glows at night and is easier to spot than to pin.",
                Prefabs = new[] { "Pickable_Thistle" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Carrot Seeds",
                Description = "Wild carrot seeds. Rare, and the only way to start a carrot farm.",
                Prefabs = new[] { "Pickable_SeedCarrot" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Forage,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Greydwarf Nests",
                Description = "The nests that keep sending greydwarfs at you. Break one for an ancient seed.",
                NameToken = "Greydwarf Nest",
                Prefabs = new[] { "Spawner_GreydwarfNest" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Spawner,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Greydwarf Runestones",
                Description = "Runestones ringed by greydwarf spawners.",
                Prefabs = new[] { "Runestone_Greydwarfs" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Spawner,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Burial Chambers",
                Description = "The entrances to burial chambers, where the surtling cores are.",
                Prefabs = new[] { "Crypt2", "Crypt3", "Crypt4" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Dungeon,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Troll Caves",
                Description = "Troll cave entrances.",
                Prefabs = new[] { "TrollCave02" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Dungeon,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Bear Caves",
                Description = "Bear cave entrances.",
                Prefabs = new[] { "BearCave" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Dungeon,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Mushrooms",
                Description = "Ordinary mushrooms.",
                Prefabs = new[] { "Pickable_Mushroom" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Altar of the Elder",
                Description = "Where the Elder is summoned. Off by default: use a vegvisir and the game pins it for you.",
                Prefabs = new[] { "GDKing" },
                Biome = Heightmap.Biome.BlackForest,
                Category = PinCategory.Boss,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Vegvisirs",
                Description = "The runestones that reveal a boss on your map.",
                NameToken = "$piece_vegvisir",
                Prefabs = new[] { "Vegvisir_Eikthyr", "Vegvisir_GDKing", "Vegvisir_Bonemass", "Vegvisir_DragonQueen", "Vegvisir_GoblinKing", "Vegvisir_Fader", "Vegvisir_DNBoss" },
                Biome = Heightmap.Biome.Swamp,
                Category = PinCategory.Vegvisir,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Turnip Seeds",
                Description = "Wild turnip seeds. Rare, and the only way to start a turnip farm.",
                Prefabs = new[] { "Pickable_SeedTurnip" },
                Biome = Heightmap.Biome.Swamp,
                Category = PinCategory.Forage,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Thistle",
                Description = "Thistle.",
                Prefabs = new[] { "Pickable_Thistle" },
                Biome = Heightmap.Biome.Swamp,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Sunken Crypts",
                Description = "The iron doors into the sunken crypts, where the iron scrap is.",
                Prefabs = new[] { "SunkenCrypt4" },
                Biome = Heightmap.Biome.Swamp,
                Category = PinCategory.Dungeon,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Muddy Scrap Piles",
                Description = "The mud piles you dig for iron scrap. Only inside the crypts, so rarely worth a pin.",
                Prefabs = new[] { "mudpile_beacon" },
                Biome = Heightmap.Biome.Swamp,
                Category = PinCategory.Ore,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Surtling Fire Holes",
                Description = "The burning holes that keep producing surtlings.",
                Prefabs = new[] { "FireHole" },
                Biome = Heightmap.Biome.Swamp,
                Category = PinCategory.Spawner,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Draugr Runestones",
                Description = "Runestones ringed by draugr spawners.",
                Prefabs = new[] { "Runestone_Draugr" },
                Biome = Heightmap.Biome.Swamp,
                Category = PinCategory.Spawner,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Mushrooms",
                Description = "Ordinary mushrooms.",
                Prefabs = new[] { "Pickable_Mushroom" },
                Biome = Heightmap.Biome.Swamp,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Altar of Bonemass",
                Description = "Where Bonemass is summoned. Off by default: use a vegvisir and the game pins it for you.",
                Prefabs = new[] { "Bonemass" },
                Biome = Heightmap.Biome.Swamp,
                Category = PinCategory.Boss,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Vegvisirs",
                Description = "The runestones that reveal a boss on your map.",
                NameToken = "$piece_vegvisir",
                Prefabs = new[] { "Vegvisir_Eikthyr", "Vegvisir_GDKing", "Vegvisir_Bonemass", "Vegvisir_DragonQueen", "Vegvisir_GoblinKing", "Vegvisir_Fader", "Vegvisir_DNBoss" },
                Biome = Heightmap.Biome.Mountain,
                Category = PinCategory.Vegvisir,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Silver Deposits",
                Description = "Silver under the snow. Buried, so a pin is worth more here than anywhere else.",
                Prefabs = new[] { "silvervein" },
                Biome = Heightmap.Biome.Mountain,
                Category = PinCategory.Ore,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Obsidian Deposits",
                Description = "The black spires you mine for obsidian.",
                Prefabs = new[] { "MineRock_Obsidian" },
                Biome = Heightmap.Biome.Mountain,
                Category = PinCategory.Ore,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Drake Nests",
                Description = "The nests on the peaks, each holding a dragon egg.",
                Prefabs = new[] { "DrakeNest01" },
                Biome = Heightmap.Biome.Mountain,
                Category = PinCategory.Forage,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Altar of Moder",
                Description = "Where Moder is summoned. Off by default: use a vegvisir and the game pins it for you.",
                Prefabs = new[] { "Dragonqueen" },
                Biome = Heightmap.Biome.Mountain,
                Category = PinCategory.Boss,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Vegvisirs",
                Description = "The runestones that reveal a boss on your map.",
                NameToken = "$piece_vegvisir",
                Prefabs = new[] { "Vegvisir_Eikthyr", "Vegvisir_GDKing", "Vegvisir_Bonemass", "Vegvisir_DragonQueen", "Vegvisir_GoblinKing", "Vegvisir_Fader", "Vegvisir_DNBoss" },
                Biome = Heightmap.Biome.Plains,
                Category = PinCategory.Vegvisir,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Tar Pits",
                Description = "The black pools, with growths to pick and a nest of tar blobs guarding them.",
                Prefabs = new[] { "TarPit1", "TarPit1_1", "TarPit2", "TarPit2_1", "TarPit3", "TarPit3_1" },
                Biome = Heightmap.Biome.Plains,
                Category = PinCategory.Tar,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Fuling Totems",
                Description = "The totem in a fuling village, and the only way to summon Yagluth. Pinned instead of the village around it.",
                Prefabs = new[] { "goblin_totempole" },
                Biome = Heightmap.Biome.Plains,
                Category = PinCategory.Loot,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Cloudberry Bushes",
                Description = "Cloudberry bushes.",
                Prefabs = new[] { "CloudberryBush" },
                Biome = Heightmap.Biome.Plains,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Altar of Yagluth",
                Description = "Where Yagluth is summoned. Off by default: use a vegvisir and the game pins it for you.",
                Prefabs = new[] { "GoblinKing" },
                Biome = Heightmap.Biome.Plains,
                Category = PinCategory.Boss,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Yggdrasil Roots",
                Description = "The hanging roots you tap for sap.",
                Prefabs = new[] { "YggdrasilRoot" },
                Biome = Heightmap.Biome.Mistlands,
                Category = PinCategory.Sap,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Giant Remains",
                Description = "The bones and armour of the fallen giants. Black marble, and iron and copper scrap.",
                NameToken = "Giant Remains",
                Prefabs = new[] { "giant_ribs", "giant_helmet1", "giant_helmet2", "giant_sword1", "giant_sword2" },
                Biome = Heightmap.Biome.Mistlands,
                Category = PinCategory.Ore,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Magecap",
                Description = "The blue glowing mushrooms, for eitr.",
                Prefabs = new[] { "Pickable_Mushroom_Magecap" },
                Biome = Heightmap.Biome.Mistlands,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Jotun Puffs",
                Description = "The pale puffball mushrooms, for eitr.",
                Prefabs = new[] { "Pickable_Mushroom_JotunPuffs" },
                Biome = Heightmap.Biome.Mistlands,
                Category = PinCategory.Forage,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Entrance to the Queen",
                Description = "The way into the infested mine where the Queen waits.",
                Prefabs = new[] { "Mistlands_DvergrBossEntrance1" },
                Biome = Heightmap.Biome.Mistlands,
                Category = PinCategory.Dungeon,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Dvergr Needle Crates",
                Description = "The long crate holding a dvergr needle, which is what a sap extractor is built from. One stands in each guard tower, excavation, harbour and lighthouse.",
                Prefabs = new[] { "dvergrprops_crate_long" },
                Biome = Heightmap.Biome.Mistlands,
                Category = PinCategory.Loot,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Dvergr Excavations",
                Description = "Dvergr dig sites, with crates to break and dvergr who mind you breaking them.",
                Prefabs = new[] { "Mistlands_Excavation1", "Mistlands_Excavation2", "Mistlands_Excavation3" },
                Biome = Heightmap.Biome.Mistlands,
                Category = PinCategory.Loot,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Vegvisirs",
                Description = "The runestones that reveal a boss on your map.",
                NameToken = "$piece_vegvisir",
                Prefabs = new[] { "Vegvisir_Eikthyr", "Vegvisir_GDKing", "Vegvisir_Bonemass", "Vegvisir_DragonQueen", "Vegvisir_GoblinKing", "Vegvisir_Fader", "Vegvisir_DNBoss" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Vegvisir,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Flametal",
                Description = "The flametal in the lava lakes.",
                Prefabs = new[] { "LeviathanLava" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Ore,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Sulfur Arches",
                Description = "The yellow arches you pick for sulfur stone.",
                Prefabs = new[] { "SulfurArch" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Ore,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Unstable Lava Rock",
                Description = "The cracked rocks holding proustite powder.",
                Prefabs = new[] { "UnstableLavaRock" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Ore,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Vineberries",
                Description = "The vines in the charred ruins. Vineberries, which can be replanted.",
                Prefabs = new[] { "VineAsh" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Forage,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Smoke Puffs",
                Description = "The smoking mushrooms.",
                Prefabs = new[] { "Pickable_SmokePuff" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Ash Pots",
                Description = "The red clay pots, which break open for bronze and iron.",
                NameToken = "Ash Pot",
                Prefabs = new[] { "ashland_pot2_red" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Loot,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Places of Mystery",
                Description = "Where the pieces of Dyrnwyn are. Three in the world, and no way to finish the sword without them.",
                Prefabs = new[] { "PlaceofMystery1", "PlaceofMystery2", "PlaceofMystery3" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Loot,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Charred Fortresses",
                Description = "The fortresses, holding a molten core, a bell fragment and a great many charred.",
                Prefabs = new[] { "CharredFortress" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Dungeon,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Charred Skulls",
                Description = "Skulls on the ground, picked for charred skull.",
                Prefabs = new[] { "Pickable_Charredskull" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Volture Nests",
                Description = "Volture nests, built on asksvin carrion worth picking over.",
                Prefabs = new[] { "VoltureNest" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Loot,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Charred Tower Ruins",
                Description = "Ruined towers with pots and fiddlehead.",
                Prefabs = new[] { "CharredTowerRuins1" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Loot,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Charred Spawner Towers",
                Description = "Ruined towers built around a charred spawner.",
                Prefabs = new[] { "CharredTowerRuins3" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Spawner,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Dvergr Town",
                Description = "The dvergr settlements out on the ash sea.",
                Prefabs = new[] { "CharredTowerRuins1_dvergr" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Loot,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Altar of Fader",
                Description = "Where Fader is summoned. Off by default: use a vegvisir and the game pins it for you.",
                Prefabs = new[] { "FaderLocation" },
                Biome = Heightmap.Biome.AshLands,
                Category = PinCategory.Boss,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Vegvisirs",
                Description = "The runestones that reveal a boss on your map.",
                NameToken = "$piece_vegvisir",
                Prefabs = new[] { "Vegvisir_Eikthyr", "Vegvisir_GDKing", "Vegvisir_Bonemass", "Vegvisir_DragonQueen", "Vegvisir_GoblinKing", "Vegvisir_Fader", "Vegvisir_DNBoss" },
                Biome = Heightmap.Biome.DeepNorth,
                Category = PinCategory.Vegvisir,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Frozen Trolls",
                Description = "The trolls frozen into the ice, mined for gold ore. Needs a black metal pickaxe.",
                Prefabs = new[] { "DN_gammeltrollFrac01", "DN_gammeltrollFrac02" },
                Biome = Heightmap.Biome.DeepNorth,
                Category = PinCategory.Ore,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Morkhalla",
                Description = "The black keeps, holding the ancient gemstones.",
                Prefabs = new[] { "MorkBorg" },
                Biome = Heightmap.Biome.DeepNorth,
                Category = PinCategory.Dungeon,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Memorial Places",
                Description = "Eleven chests around a runestone and an offering bowl.",
                Prefabs = new[] { "NorthMemorialPlace" },
                Biome = Heightmap.Biome.DeepNorth,
                Category = PinCategory.Loot,
                DefaultOn = true,
                IsLocation = true
            },
            new Rule
            {
                Name = "Kale Seeds",
                Description = "Wild kale seeds. Rare, and the only way to start a kale farm.",
                Prefabs = new[] { "Pickable_SeedKale" },
                Biome = Heightmap.Biome.DeepNorth,
                Category = PinCategory.Forage,
                DefaultOn = true,
                IsLocation = false
            },
            new Rule
            {
                Name = "Lingonberry Bushes",
                Description = "Lingonberry bushes.",
                Prefabs = new[] { "LingonberryBush" },
                Biome = Heightmap.Biome.DeepNorth,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "Snowballs",
                Description = "Snowballs.",
                Prefabs = new[] { "Pickable_Snowball" },
                Biome = Heightmap.Biome.DeepNorth,
                Category = PinCategory.Forage,
                DefaultOn = false,
                IsLocation = false
            },
            new Rule
            {
                Name = "The Hole",
                Description = "A camp built around a hole in the ice, with real goods in it.",
                Prefabs = new[] { "TheHole01" },
                Biome = Heightmap.Biome.DeepNorth,
                Category = PinCategory.Loot,
                DefaultOn = false,
                IsLocation = true
            },
            new Rule
            {
                Name = "Leviathans",
                Description = "The sleeping leviathans, mined for chitin. They dive once you start.",
                NameToken = "Leviathan",
                Prefabs = new[] { "Leviathan" },
                Biome = Heightmap.Biome.Ocean,
                Category = PinCategory.Ore,
                DefaultOn = true,
                IsLocation = false
            },
        };
    }
}
