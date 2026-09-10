using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using BetterMap.Scripts;
using BetterMap.Scripts.Creatures;
using BetterMap.Scripts.Map;
using BetterMap.Scripts.Vehicles;
using UnityEngine;

namespace BetterMap
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        // Entities
        public static ConfigEntry<bool> showCreatures;
        public static ConfigEntry<float> creatureRefreshInterval;
        public static ConfigEntry<bool> showEntityNames;
        public static ConfigEntry<bool> tintTamedCreatures;
        public static ConfigEntry<bool> tintHostileCreatures;

        // Vehicles
        public static ConfigEntry<bool> showBoats;
        public static ConfigEntry<bool> showCarts;
        public static ConfigEntry<bool> rotateBoatIcons;
        public static ConfigEntry<bool> showVehicleNames;
        public static ConfigEntry<float> vehicleRefreshInterval;

        // Auto pins
        public static ConfigEntry<bool> autoPinResources;
        public static ConfigEntry<bool> autoPinLocations;
        public static ConfigEntry<bool> autoPinPortals;
        public static ConfigEntry<float> autoPinMergeDistance;

        // Map
        public static ConfigEntry<int> deathMarkersKept;
        public static ConfigEntry<float> explorationRadius;
        public static ConfigEntry<float> iconScale;

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
            DeathMarkers.Tick();
            VehicleTracker.Tick();
            VehicleIndex.Tick();
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

            tintHostileCreatures = ConfigSync("Creatures", "Tint Hostile Creatures", true,
                new ConfigDescription(
                    "Tint red the pins of creatures that will attack you on sight. Only applies to creatures with no trophy to use as an icon, which are drawn with a plain pin: a creature drawn as its own trophy is already recognisable."));

            // Vehicles -------------------------------------------------------

            showBoats = ConfigSync("Vehicles", "Show Boats", true,
                new ConfigDescription(
                    "Show boats on the map. Boats are found anywhere in the world, not only near you, which is what makes one you left adrift findable. Playing alone or hosting, that means every boat there is. As a guest on a server you get the ones in every area you have loaded since connecting, and a boat far away sits where it was when you were last near it."));

            showCarts = ConfigSync("Vehicles", "Show Carts", true,
                new ConfigDescription(
                    "Show carts on the map, wherever you left them."));

            rotateBoatIcons = ConfigSync("Vehicles", "Rotate Boat Icons", true,
                new ConfigDescription(
                    "Turn boat icons to point the way the boat is facing. While you are sailing, this also replaces the game's own boat marker so both look the same."));

            showVehicleNames = ConfigSync("Vehicles", "Show Vehicle Names", true,
                new ConfigDescription(
                    "Show what each vehicle is under its icon, so a raft can be told from a longship."));

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
                    "How many of your death markers to keep on the map. The game drops a marker where you died but never saves it, so without this they are gone the next time you load the world.",
                    new AcceptableValueRange<int>(1, 20)));

            // Also the radius creatures are tracked within, deliberately not mentioned: keeping the
            // two the same is what stops a creature being pinned on ground that is still black, and
            // it is one less number for anyone to get wrong.
            explorationRadius = ConfigSync("Map", "Exploration Radius", 100f,
                new ConfigDescription(
                    "How much of the map is uncovered as you walk, in meters. 100 is the vanilla value.",
                    new AcceptableValueRange<float>(20f, 500f)));

            iconScale = ConfigSync("Map", "Icon Scale", 1.25f,
                new ConfigDescription(
                    "Size of every pin on the map, as a multiple of its normal size. 1 leaves the game's own size, 32 pixels on the minimap and 48 on the map.",
                    new AcceptableValueRange<float>(0.5f, 3f)));

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
