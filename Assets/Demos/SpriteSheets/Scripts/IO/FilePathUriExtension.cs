using System;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public static class FilePathUriExtension
    {
        public static bool TryGetFileUri(this string path, out string fileUri)
        {
            fileUri = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                fileUri = FormatUri(path);
                return string.IsNullOrEmpty(fileUri) == false;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        private static string FormatUri(string path)
        {
            if (path.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return new Uri(path).AbsoluteUri;
        }
    }
}
