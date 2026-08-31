using System;
using System.Collections;
using System.IO;
using UnityEngine.Networking;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public static class SpriteSheetFile
    {
        public const string FILE_EXTENSION = "spritesheet";

        public static IEnumerator LoadAsync(string path, Action<SpriteSheet> onComplete)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path is empty", nameof(path));
            }

            yield return LoadAsyncInternal(path, onComplete);
        }

        private static IEnumerator LoadAsyncInternal(string path, Action<SpriteSheet> onComplete)
        {
            if (path.TryGetFileUri(out string fileUri) == false)
            {
                yield break;
            }

            UnityWebRequest request = UnityWebRequest.Get(fileUri);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }

            SpriteSheet spriteSheet;

            try
            {
                spriteSheet = SpriteSheet.Load(request.downloadHandler.data);
            }
            catch
            {
                yield break;
            }

            onComplete?.Invoke(spriteSheet);
        }
    }
}
