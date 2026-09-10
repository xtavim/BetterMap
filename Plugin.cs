using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using BetterMap.Scripts;
using BetterMap.Scripts.Creatures;
using UnityEngine;

namespace BetterMap
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        // Entities
        public static ConfigEntry<bool> showCreatures;
        public static ConfigEntry<float> creatureRadius;
        public static ConfigEntry<float> creatureRefreshInterval;
        public static ConfigEntry<bool> showEntityNames;
        public static ConfigEntry<bool> tintTamedCreatures;

        // Vehicles
        public static ConfigEntry<bool> showBoats;
        public static ConfigEntry<bool> showCarts;
        public static ConfigEntry<float> vehicleRefreshInterval;

        // Auto pins
        public static ConfigEntry<bool> autoPinResources;
        public static ConfigEntry<bool> autoPinLocations;
        public static ConfigEntry<bool> autoPinPortals;
        public static ConfigEntry<float> autoPinMergeDistance;

        // Map
        public static ConfigEntry<int> deathMarkersKept;
        public static ConfigEntry<float> explorationRadius;

        public static ConfigEntry<bool> debugMode;
        public static ConfigEntry<bool> dumpPrefabs;

        public new static readonly ManualLogSource Logger =
            BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

        private static readonly ConfigSync configSync = new(PluginInfo.PLUGIN_GUID)
        {
            DisplayName = PluginInfo.PLUGIN_NAME,
            CurrentVersion = PluginInfo.PLUGIN_VERSION,
            MinimumRequiredVersion = PluginInfo.PLUGIN_VERSION
        };

        private void Awake()
        {
            InitializeConfig();
            InitializeHarmonyPatches();

            Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} {PluginInfo.PLUGIN_VERSION} loaded");
        }

        private void Update()
        {
            VegetationDumper.TryDump();
            CreatureTracker.Tick();
        }

        private ConfigEntry<T> ConfigSync<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            var configDescription = new ConfigDescription(
                description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
            var configEntry = Config.Bind(group, name, value, configDescription);
            configSync.AddConfigEntry(configEntry).SynchronizedConfig = synchronizedSetting;
            return configEntry;
        }

        private void InitializeConfig()
        {
            Config.SaveOnConfigSet = false;

            var serverConfigLocked = ConfigSync("1 - ServerSync", "Lock Configuration", true,
                new ConfigDescription(
                    "If enabled, the configuration is locked and can be changed by server admins only."));
            configSync.AddLockingConfigEntry(serverConfigLocked);

            // Creatures ------------------------------------------------------

            showCreatures = ConfigSync("Creatures", "Show Creatures", true,
                new ConfigDescription(
                    "Show creatures on the minimap and the map, using their trophy as the icon."));

            creatureRadius = ConfigSync("Creatures", "Creature Radius", 60f,
                new ConfigDescription(
                    "How far from the player, in meters, creatures are tracked. Creatures outside this are not drawn and are not processed at all.",
                    new AcceptableValueRange<float>(10f, 300f)));

            creatureRefreshInterval = ConfigSync("Creatures", "Creature Refresh Interval", 0.5f,
                new ConfigDescription(
                    "How often, in seconds, the set of tracked creatures is rebuilt. Their pins follow them every frame regardless.",
                    new AcceptableValueRange<float>(0.1f, 5f)));

            showEntityNames = ConfigSync("Creatures", "Show Creature Names", false,
                new ConfigDescription(
                    "Show a name under every creature pin. Creatures with no trophy icon, and tamed creatures that have been given a name, always show theirs regardless of this setting."));

            tintTamedCreatures = ConfigSync("Creatures", "Tint Tamed Creatures", true,
                new ConfigDescription(
                    "Tint the pins of tamed creatures green so they stand out from the wildlife."));

            // Vehicles -------------------------------------------------------

            showBoats = ConfigSync("Vehicles", "Show Boats", true,
                new ConfigDescription(
                    "Show boats on the map, rotated to their heading. Boats are tracked anywhere in the world, not only near the player."));

            showCarts = ConfigSync("Vehicles", "Show Carts", true,
                new ConfigDescription(
                    "Show carts on the map, wherever they are in the world."));

            vehicleRefreshInterval = ConfigSync("Vehicles", "Vehicle Refresh Interval", 5f,
                new ConfigDescription(
                    "How often, in seconds, vehicles anywhere in the world are looked up. Vehicles near the player follow them continuously.",
                    new AcceptableValueRange<float>(1f, 60f)));

            // Auto pins ------------------------------------------------------

            autoPinResources = ConfigSync("Auto Pins", "Pin Resources", true,
                new ConfigDescription(
                    "Pin resources as you find them: ore deposits, harvestable plants and the like."));

            autoPinLocations = ConfigSync("Auto Pins", "Pin Locations", true,
                new ConfigDescription(
                    "Pin dungeons, crypts, caves and other locations as you discover them."));

            autoPinPortals = ConfigSync("Auto Pins", "Pin Portals", true,
                new ConfigDescription(
                    "Pin portals with their tag as the pin name."));

            autoPinMergeDistance = ConfigSync("Auto Pins", "Merge Distance", 5f,
                new ConfigDescription(
                    "Do not place an automatic pin within this many meters of one that is already there, so walking past the same deposit does not stack pins on it.",
                    new AcceptableValueRange<float>(1f, 50f)));

            // Map ------------------------------------------------------------

            deathMarkersKept = ConfigSync("Map", "Death Markers Kept", 3,
                new ConfigDescription(
                    "How many death markers to keep. Vanilla keeps only the most recent one.",
                    new AcceptableValueRange<int>(1, 20)));

            explorationRadius = ConfigSync("Map", "Exploration Radius", 100f,
                new ConfigDescription(
                    "How much of the map is uncovered as you walk, in meters. 100 is the vanilla value.",
                    new AcceptableValueRange<float>(20f, 500f)));

            debugMode = ConfigSync("Debug", "Debug Mode", false,
                new ConfigDescription(
                    "Log what is being pinned and tracked."), false);

            dumpPrefabs = ConfigSync("Debug", "Dump Prefabs", false,
                new ConfigDescription(
                    "Write every harvestable, creature, location and vehicle prefab in the installed game to BetterMap.prefabs.txt next to this config. Runs once per game start. Diagnostic only."), false);

            Config.SaveOnConfigSet = true;
            Config.Save();
        }

        private static void InitializeHarmonyPatches()
        {
            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
        }
    }
}
