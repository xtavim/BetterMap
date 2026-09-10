using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts
{
    public static class Icons
    {
        private static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        private static readonly MethodInfo LoadImageMethod = FindLoadImage();

        // Reflected rather than referenced: the module holding LoadImage is built against a newer
        // standard library than this project targets, so naming it fails the build.
        private static MethodInfo FindLoadImage()
        {
            var type = AccessTools.TypeByName("UnityEngine.ImageConversion");
            if (type == null) return null;

            return AccessTools.Method(type, "LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) })
                   ?? AccessTools.Method(type, "LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
        }

        public static Sprite Boat => Get("boat.png");
        public static Sprite Cart => Get("cart.png");

        public static Sprite For(Pins.PinCategory category)
        {
            switch (category)
            {
                case Pins.PinCategory.Ore: return Get("ore.png");
                case Pins.PinCategory.Forage: return Get("forage.png");
                case Pins.PinCategory.Dungeon: return Get("dungeon.png");
                case Pins.PinCategory.Loot: return Get("loot.png");
                case Pins.PinCategory.Spawner: return Get("spawner.png");
                case Pins.PinCategory.Vegvisir: return Get("vegvisir.png");
                case Pins.PinCategory.Beehive: return Get("beehive.png");
                case Pins.PinCategory.Tar: return Get("tar.png");
                case Pins.PinCategory.Sap: return Get("sap.png");
                default: return null;
            }
        }

        private static Sprite Get(string file)
        {
            if (_sprites.TryGetValue(file, out var cached)) return cached;

            var sprite = Load(file);
            _sprites[file] = sprite;

            return sprite;
        }

        private static Sprite Load(string file)
        {
            try
            {
                var bytes = Read(file);
                if (bytes == null)
                {
                    Plugin.Logger.LogWarning($"Icons: {file} is not in the assembly");
                    return null;
                }

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                if (!Decode(texture, bytes))
                {
                    Plugin.Logger.LogWarning($"Icons: {file} could not be decoded");
                    return null;
                }

                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"Icons: {file} failed to load: {e}");
                return null;
            }
        }

        private static bool Decode(Texture2D texture, byte[] bytes)
        {
            if (LoadImageMethod == null)
            {
                Plugin.Logger.LogWarning("Icons: the game's image decoder was not found");
                return false;
            }

            var args = LoadImageMethod.GetParameters().Length == 3
                ? new object[] { texture, bytes, false }
                : new object[] { texture, bytes };

            return (bool)LoadImageMethod.Invoke(null, args);
        }

        private static byte[] Read(string file)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var suffix = "." + file;

            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;

                using (var stream = assembly.GetManifestResourceStream(name))
                {
                    if (stream == null) return null;

                    var bytes = new byte[stream.Length];
                    var read = 0;

                    while (read < bytes.Length)
                    {
                        var got = stream.Read(bytes, read, bytes.Length - read);
                        if (got <= 0) break;
                        read += got;
                    }

                    return bytes;
                }
            }

            return null;
        }
    }
}
