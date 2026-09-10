using System.Collections.Generic;
using UnityEngine;

namespace BetterMap.Scripts.Vehicles
{
    /// <summary>
    /// The server's answer to "where are my vehicles".
    ///
    /// Only the server holds every ZDO in the world. A client is sent what it has been near, so on
    /// its own it can never find a boat left in a place it has not visited this session. Asking the
    /// server removes that difference, and costs nothing in single player, where the player is the
    /// server and the request never leaves the machine.
    ///
    /// Walking the world is slow, so it is done once into a cache, spread over frames, and only when
    /// somebody has actually asked. Several players asking at the same time share one walk.
    /// </summary>
    public static class VehicleIndex
    {
        public struct Record
        {
            public ZDOID Id;
            public int Prefab;
            public Vector3 Pos;
            public float Heading;
            public long Creator;
        }

        private class Request
        {
            public long Peer;
            public long Player;
            public HashSet<ZDOID> Used;
        }

        private static readonly List<Record> _records = new List<Record>();
        private static readonly List<Request> _pending = new List<Request>();

        private static readonly List<ZDO> _found = new List<ZDO>();
        private static readonly List<Record> _matched = new List<Record>();

        private static int _kindIndex;
        private static int _sectorIndex;
        private static bool _scanning;
        private static float _scannedAt = float.NegativeInfinity;

        public static void Ask(long peer, long player, HashSet<ZDOID> used)
        {
            // A walk that has only just finished is as good as a new one, and stops a room full of
            // players from keeping the server permanently scanning.
            if (!_scanning && Time.time - _scannedAt < Plugin.vehicleRefreshInterval.Value)
            {
                Answer(new Request { Peer = peer, Player = player, Used = used });
                return;
            }

            _pending.Add(new Request { Peer = peer, Player = player, Used = used });
        }

        public static void Tick()
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            if (ZDOMan.instance == null || !VehicleKinds.Ready) return;

            if (!_scanning)
            {
                if (_pending.Count == 0) return;

                _scanning = true;
                _kindIndex = 0;
                _sectorIndex = 0;
                _found.Clear();
            }

            Scan();
        }

        /// <summary>
        /// One step per frame. Each step walks up to 400 populated sectors, so a large world takes
        /// several frames and no single frame pays for the whole thing.
        /// </summary>
        private static void Scan()
        {
            var kinds = VehicleKinds.All;

            while (_kindIndex < kinds.Count)
            {
                if (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(kinds[_kindIndex].Prefab, _found, ref _sectorIndex))
                {
                    // More sectors to walk for this prefab. Pick it up next frame.
                    return;
                }

                _kindIndex++;
                _sectorIndex = 0;
                return;
            }

            Collect();

            _scanning = false;
            _scannedAt = Time.time;

            foreach (var request in _pending) Answer(request);
            _pending.Clear();
        }

        private static void Collect()
        {
            _records.Clear();

            foreach (var zdo in _found)
            {
                if (zdo == null || !zdo.IsValid()) continue;

                _records.Add(new Record
                {
                    Id = zdo.m_uid,
                    Prefab = zdo.GetPrefab(),
                    Pos = zdo.GetPosition(),
                    Heading = zdo.GetRotation().eulerAngles.y,

                    // Written by the game when the piece is placed, so this is the builder.
                    Creator = zdo.GetLong(ZDOVars.s_creator, 0L)
                });
            }

            _found.Clear();
        }

        /// <summary>
        /// Answers with what the asker is entitled to and nothing else: what they built, and what
        /// they have driven. A stranger's boat is not theirs to find from across the world.
        /// </summary>
        private static void Answer(Request request)
        {
            _matched.Clear();

            foreach (var record in _records)
            {
                var mine = record.Creator != 0L && record.Creator == request.Player;
                if (!mine && (request.Used == null || !request.Used.Contains(record.Id))) continue;

                _matched.Add(record);
            }

            var package = new ZPackage();
            package.Write(_matched.Count);

            foreach (var record in _matched)
            {
                package.Write(record.Id);
                package.Write(record.Prefab);
                package.Write(record.Pos);
                package.Write(record.Heading);
            }

            VehicleRpc.Reply(request.Peer, package);
        }
    }
}
