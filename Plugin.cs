using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using BetterMap.Scripts;
using BetterMap.Scripts.Creatures;
using BetterMap.Scripts.Map;
using BetterMap.Scripts.Pins;
using BetterMap.Scripts.Vehicles;
using UnityEngine;

namespace BetterMap
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> showCreatures;
        public static ConfigEntry<float> creatureRefreshInterval;
        public static ConfigEntry<float> creatureRadius;
        public static ConfigEntry<bool> showEntityNames;
        public static ConfigEntry<bool> tintTamedCreatures;
        public static ConfigEntry<bool> tintHostileCreatures;

        public static ConfigEntry<bool> showBoats;
        public static ConfigEntry<bool> showCarts;
        public static ConfigEntry<bool> rotateBoatIcons;
        public static ConfigEntry<bool> showVehicleNames;
        public static ConfigEntry<float> vehicleRefreshInterval;
        public static ConfigEntry<float> vehicleRadius;

        public static ConfigEntry<bool> autoPin;
        public static ConfigEntry<bool> autoPinPortals;
        public static ConfigEntry<bool> nameTraderPins;
        public static ConfigEntry<bool> revealTraders;
        public static ConfigEntry<float> autoPinMergeDistance;
        public static ConfigEntry<float> autoPinInterval;
        public static ConfigEntry<float> autoPinRadius;

        public const Minimap.PinType AutoPinType = Minimap.PinType.Icon3;

        public static ConfigEntry<int> deathMarkersKept;
        public static ConfigEntry<float> explorationRadius;
        public static ConfigEntry<float> iconScale;

        public static ConfigEntry<bool> debugMode;
        public static ConfigEntry<bool> forgetPinned;

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
            CreatureTracker.Tick();
            DeathMarkers.Tick();
            VehicleTracker.Tick();
            VehicleIndex.Tick();
            AutoPins.Tick();
            PinFilters.Tick();
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

            showCreatures = ConfigSync("Creatures", "Show Creatures", true,
                new ConfigDescription(
                    "Show creatures on the minimap and the map, using their trophy as the icon."));

            creatureRefreshInterval = ConfigSync("Creatures", "Creature Refresh Interval", 0.5f,
                new ConfigDescription(
                    "How often, in seconds, the set of tracked creatures is rebuilt. Their pins follow them every frame regardless.",
                    new AcceptableValueRange<float>(0.1f, 5f)), false);

            creatureRadius = ConfigSync("Creatures", "Creature Radius", 100f,
                new ConfigDescription(
                    "How far from you, in meters, creatures are shown. The game only loads the area around you, so past about 200 there is nothing more to see.",
                    new AcceptableValueRange<float>(10f, 200f)));

            showEntityNames = ConfigSync("Creatures", "Show Creature Names", false,
                new ConfigDescription(
                    "Show a name under every creature pin. Creatures with no trophy icon, and tamed creatures that have been given a name, always show theirs regardless of this setting."), false);

            tintTamedCreatures = ConfigSync("Creatures", "Tint Tamed Creatures", true,
                new ConfigDescription(
                    "Tint the pins of tamed creatures green so they stand out from the wildlife."), false);

            tintHostileCreatures = ConfigSync("Creatures", "Tint Hostile Creatures", true,
                new ConfigDescription(
                    "Tint red the pins of creatures that will attack you on sight. Only applies to creatures with no trophy to use as an icon, which are drawn with a plain pin: a creature drawn as its own trophy is already recognisable."), false);

            showBoats = ConfigSync("Vehicles", "Show Boats", true,
                new ConfigDescription(
                    "Show boats on the map. Boats are found anywhere in the world, not only near you, which is what makes one you left adrift findable. Playing alone or hosting, that means every boat there is. As a guest on a server you get the ones in every area you have loaded since connecting, and a boat far away sits where it was when you were last near it."));

            showCarts = ConfigSync("Vehicles", "Show Carts", true,
                new ConfigDescription(
                    "Show carts on the map, wherever you left them."));

            rotateBoatIcons = ConfigSync("Vehicles", "Rotate Boat Icons", true,
                new ConfigDescription(
                    "Turn boat icons to point the way the boat is facing. While you are sailing, this also replaces the game's own boat marker so both look the same."), false);

            showVehicleNames = ConfigSync("Vehicles", "Show Vehicle Names", true,
                new ConfigDescription(
                    "Show what each vehicle is under its icon, so a raft can be told from a longship."), false);

            vehicleRefreshInterval = ConfigSync("Vehicles", "Vehicle Refresh Interval", 5f,
                new ConfigDescription(
                    "How often, in seconds, vehicles anywhere in the world are looked up. Vehicles near the player follow them continuously.",
                    new AcceptableValueRange<float>(1f, 60f)));

            vehicleRadius = ConfigSync("Vehicles", "Vehicle Radius", 100f,
                new ConfigDescription(
                    "How far from you, in meters, vehicles are followed as they move. Past it they are shown where they were last looked up.",
                    new AcceptableValueRange<float>(10f, 200f)), false);

            autoPin = ConfigSync("Auto Pins", "Enable", true,
                new ConfigDescription(
                    "Pin resources as you come across them. What gets pinned is chosen biome by biome below, and a pin you delete is never put back."));

            autoPinInterval = ConfigSync("Auto Pins", "Sweep Interval", 2f,
                new ConfigDescription(
                    "How often, in seconds, the loaded world is looked over for anything worth pinning.",
                    new AcceptableValueRange<float>(0.5f, 30f)), false);

            autoPinRadius = ConfigSync("Auto Pins", "Pin Radius", 100f,
                new ConfigDescription(
                    "How far from you, in meters, anything worth pinning is spotted. The game only loads the area around you, so past about 200 there is nothing more to find.",
                    new AcceptableValueRange<float>(5f, 200f)));

            autoPinPortals = ConfigSync("Auto Pins", "Pin Portals", true,
                new ConfigDescription(
                    "Pin portals where they stand, named by their tag, using the game's own portal icon. Renaming a portal renames its pin."), false);

            revealTraders = ConfigSync("Auto Pins", "Reveal Traders", false,
                new ConfigDescription(
                    "Put every trader in the world on your map at once, Haldor and Hildir and the bog witch, without having to stumble across them. Off by default because finding them is part of the game. On a server this is decided by the server, and takes effect the next time you connect."));

            nameTraderPins = ConfigSync("Auto Pins", "Name Trader Pins", true,
                new ConfigDescription(
                    "Put the trader's name on the pin the game already places for them, so Haldor, Hildir and the bog witch can be told apart on the map."), false);

            autoPinMergeDistance = ConfigSync("Auto Pins", "Merge Distance", 5f,
                new ConfigDescription(
                    "Do not place an automatic pin within this many meters of one that is already there, so walking past the same deposit does not stack pins on it.",
                    new AcceptableValueRange<float>(1f, 50f)), false);

            deathMarkersKept = ConfigSync("Map", "Death Markers Kept", 3,
                new ConfigDescription(
                    "How many of your death markers to keep on the map. The game drops a marker where you died but never saves it, so without this they are gone the next time you load the world.",
                    new AcceptableValueRange<int>(1, 20)), false);

            explorationRadius = ConfigSync("Map", "Exploration Radius", 100f,
                new ConfigDescription(
                    "How much of the map is uncovered as you walk, in meters. 100 is the vanilla value. Creatures, vehicles and auto pins each have a radius of their own.",
                    new AcceptableValueRange<float>(20f, 500f)));

            iconScale = ConfigSync("Map", "Icon Scale", 1.25f,
                new ConfigDescription(
                    "Size of every pin on the map, as a multiple of its normal size. 1 leaves the game's own size, 32 pixels on the minimap and 48 on the map.",
                    new AcceptableValueRange<float>(0.5f, 3f)), false);

            BindPinRules();

            debugMode = ConfigSync("Debug", "Debug Mode", false,
                new ConfigDescription(
                    "Log what is being pinned and tracked."), false);

            forgetPinned = ConfigSync("Debug", "Forget Pinned Places", false,
                new ConfigDescription(
                    "Once, on the next world you load, throw away this character's record of where it has already pinned, so everything is pinned again. Turn it back off afterwards. This also undoes every automatic pin you deleted on purpose."), false);

            Config.SaveOnConfigSet = true;
            Config.Save();
        }

        // A rule's name is its config key, and BepInEx refuses a handful of characters in one. An
        // apostrophe in something like "Hildir's Cave" would otherwise throw here, in Awake, and take
        // the whole configuration down with it rather than the one checkbox.
        private const string ForbiddenInKeys = "=\n\t\\\"'[]";

        private void BindPinRules()
        {
            foreach (var rule in PinRules.All)
            {
                if (rule.Name == null || rule.Name.IndexOfAny(ForbiddenInKeys.ToCharArray()) >= 0)
                {
                    Logger.LogError(
                        $"PinRules: \"{rule.Name}\" cannot be a setting, it uses one of {ForbiddenInKeys}. Skipped.");
                    continue;
                }

                rule.Enabled = ConfigSync($"Auto Pins - {rule.Biome}", rule.Name, rule.DefaultOn,
                    new ConfigDescription(rule.Description), false);
            }
        }

        private static void InitializeHarmonyPatches()
        {
            new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
        }
    }
}
