using System.Collections.Generic;

namespace BetterMap.Scripts.Vehicles
{
    /// <summary>
    /// Which prefabs are vehicles, taken from the components they carry rather than a list of names,
    /// so a boat added by an update or by another mod is picked up without anything here changing.
    ///
    /// Needed on both sides: the client to draw the right icon, the server to know what to look for.
    /// </summary>
    public static class VehicleKinds
    {
        public class Kind
        {
            public string Prefab;
            public int Hash;
            public bool IsBoat;
            public string Label;
        }

        private static List<Kind> _all;
        private static Dictionary<int, Kind> _byHash;

        public static bool Ready => Discover() != null;

        public static List<Kind> All => Discover() ?? Empty;

        private static readonly List<Kind> Empty = new List<Kind>();

        public static Kind Get(int hash)
        {
            if (Discover() == null) return null;

            return _byHash.TryGetValue(hash, out var kind) ? kind : null;
        }

        private static List<Kind> Discover()
        {
            if (_all != null) return _all;

            var scene = ZNetScene.instance;
            if (scene == null || scene.m_prefabs == null) return null;

            var all = new List<Kind>();
            var byHash = new Dictionary<int, Kind>();

            foreach (var prefab in scene.m_prefabs)
            {
                if (prefab == null) continue;

                var boat = prefab.GetComponent<Ship>() != null;
                if (!boat && prefab.GetComponent<Vagon>() == null) continue;

                // The name the build menu gives the piece, which is what a player calls it. A
                // dedicated server has no localisation to hand, and does not need one: only the
                // client ever draws this.
                var piece = prefab.GetComponent<Piece>();
                var label = piece != null && !string.IsNullOrEmpty(piece.m_name) && Localization.instance != null
                    ? Localization.instance.Localize(piece.m_name)
                    : null;

                var kind = new Kind
                {
                    Prefab = prefab.name,
                    Hash = prefab.name.GetStableHashCode(),
                    IsBoat = boat,
                    Label = label
                };

                all.Add(kind);
                byHash[kind.Hash] = kind;
            }

            if (all.Count == 0) return null;

            _all = all;
            _byHash = byHash;

            if (Plugin.debugMode.Value)
            {
                foreach (var kind in all)
                {
                    Plugin.Logger.LogInfo(
                        $"VehicleKinds: {kind.Prefab} ({(kind.IsBoat ? "boat" : "cart")}, {kind.Label ?? "unnamed"})");
                }
            }

            return _all;
        }
    }
}
