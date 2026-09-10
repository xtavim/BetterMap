using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BetterMap.Scripts
{
    /// <summary>
    /// Sprites shipped with the mod, drawn for the map rather than borrowed from somewhere else.
    ///
    /// The images live inside the assembly, so there are no loose files to lose and nothing to load
    /// from disk at the wrong moment. Plain Unity does the rest: a texture, the bytes decoded into
    /// it, and a sprite over the whole thing. No asset bundle, and nothing here needs Jotunn.
    ///
    /// Only pins that are not saved can use these. The map stores a pin as name, position, type,
    /// checked, owner and author, and no sprite, so a saved pin comes back with whatever icon its
    /// type carries.
    /// </summary>
    public static class Icons
    {
        private static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        // Called through reflection rather than referenced. The module holding it is built against a
        // newer standard library than this project targets, so naming it in the project file fails
        // the build even though the call itself is fine at runtime.
        private static readonly MethodInfo LoadImageMethod = FindLoadImage();

        private static MethodInfo FindLoadImage()
        {
            var type = AccessTools.TypeByName("UnityEngine.ImageConversion");
            if (type == null) return null;

            return AccessTools.Method(type, "LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) })
                   ?? AccessTools.Method(type, "LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
        }

        public static Sprite Boat => Get("boat.png");
        public static Sprite Cart => Get("cart.png");

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

                // Size and format are replaced by LoadImage from the file itself.
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

        /// <summary>
        /// Matched on the end of the name rather than the whole of it, because the full name is
        /// built by the build from the namespace and folder and is not worth depending on.
        /// </summary>
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
