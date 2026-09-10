using System.Collections.Generic;
using UnityEngine;

namespace BetterMap.Scripts.Vehicles
{
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

        private static void Scan()
        {
            var kinds = VehicleKinds.All;

            while (_kindIndex < kinds.Count)
            {
                if (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(kinds[_kindIndex].Prefab, _found, ref _sectorIndex))
                {
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

                    Creator = zdo.GetLong(ZDOVars.s_creator, 0L)
                });
            }

            _found.Clear();
        }

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
