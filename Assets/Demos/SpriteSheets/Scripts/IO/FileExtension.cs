using System.Collections.Generic;
using System.Linq;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    internal static class FileExtension
    {
        public static string Normalize(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return string.Empty;
            }

            return extension.Trim().TrimStart('*', '.');
        }

        public static string Join(string[] extensions)
        {
            if (extensions is not { Length: > 0 })
            {
                return string.Empty;
            }

            var parts = new List<string>(extensions.Length);
            parts.AddRange(extensions.Select(Normalize)
                                                .Where(ext => ext.Length > 0));

            return string.Join(",", parts);
        }
    }
}
