using System.Collections.Generic;
using UnityEngine;

namespace BetterMap.Scripts.Vehicles
{
    /// <summary>
    /// The client asks the server where its vehicles are; the server answers.
    ///
    /// One path for every mode. Playing alone the request never leaves the machine, because the
    /// player is the server, so there is no single player branch to keep working separately.
    ///
    /// If the server does not have the mod nothing answers, and the map falls back to the vehicles
    /// near the player. That is a smaller map, not a broken one.
    /// </summary>
    public static class VehicleRpc
    {
        private const string RequestName = "BetterMap_VehiclesRequest";
        private const string ResponseName = "BetterMap_VehiclesResponse";

        public static readonly List<VehicleIndex.Record> Remote = new List<VehicleIndex.Record>();

        private static bool _registered;
        private static float _nextRequest;

        public static void Register()
        {
            if (_registered || ZRoutedRpc.instance == null) return;

            _registered = true;

            ZRoutedRpc.instance.Register<ZPackage>(RequestName, OnRequest);
            ZRoutedRpc.instance.Register<ZPackage>(ResponseName, OnResponse);
        }

        public static void Reset()
        {
            _registered = false;
            _nextRequest = 0f;
            Remote.Clear();
        }

        public static void Tick()
        {
            if (ZRoutedRpc.instance == null || Player.m_localPlayer == null) return;
            if (Time.time < _nextRequest) return;

            _nextRequest = Time.time + Plugin.vehicleRefreshInterval.Value;

            var package = new ZPackage();
            package.Write(Player.m_localPlayer.GetPlayerID());

            var used = VehicleMemory.Used();
            package.Write(used.Count);

            foreach (var id in used) package.Write(id);

            // No target: this goes to the server, whoever that is.
            ZRoutedRpc.instance.InvokeRoutedRPC(RequestName, package);
        }

        private static void OnRequest(long sender, ZPackage package)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            if (package == null) return;

            var player = package.ReadLong();
            var count = package.ReadInt();

            var used = new HashSet<ZDOID>();
            for (var i = 0; i < count; i++) used.Add(package.ReadZDOID());

            VehicleIndex.Ask(sender, player, used);
        }

        public static void Reply(long peer, ZPackage package)
        {
            if (ZRoutedRpc.instance == null) return;

            ZRoutedRpc.instance.InvokeRoutedRPC(peer, ResponseName, package);
        }

        private static void OnResponse(long sender, ZPackage package)
        {
            if (package == null) return;

            Remote.Clear();

            var count = package.ReadInt();

            for (var i = 0; i < count; i++)
            {
                Remote.Add(new VehicleIndex.Record
                {
                    Id = package.ReadZDOID(),
                    Prefab = package.ReadInt(),
                    Pos = package.ReadVector3(),
                    Heading = package.ReadSingle()
                });
            }

            if (Plugin.debugMode.Value) Plugin.Logger.LogInfo($"VehicleRpc: {count} of my vehicles");
        }
    }
}
