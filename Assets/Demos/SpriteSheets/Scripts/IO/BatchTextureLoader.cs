using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public sealed class BatchTextureLoader
    {
        private readonly IReadOnlyList<string> _diskPaths;
        private readonly MonoBehaviour _coroutineContext;

        private Texture2D[] _textures;
        private bool _isLoading;

        public BatchTextureLoader(IEnumerable<string> diskPaths, MonoBehaviour coroutineContext)
        {
            this._diskPaths = new List<string>(diskPaths ?? throw new ArgumentNullException(nameof(diskPaths)));
            this._coroutineContext = coroutineContext ?? throw new ArgumentNullException(nameof(coroutineContext));
        }

        public IReadOnlyList<Texture2D> Textures => this._textures;

        public IEnumerator LoadAllAsync()
        {
            if (this._isLoading)
            {
                while (this._isLoading)
                {
                    yield return null;
                }

                yield break;
            }

            this._isLoading = true;

            try
            {
                int count = this._diskPaths.Count;
                this._textures = new Texture2D[count];

                int completedLoadRequests = 0;

                for (int i = 0; i < count; i++)
                {
                    this._coroutineContext.StartCoroutine(
                        LoadTextureAtPath(this._diskPaths[i], i, OnComplete, OnFail));
                }

                while (completedLoadRequests < count)
                {
                    yield return null;
                }

                void OnComplete(int index, Texture2D texture)
                {
                    this._textures[index] = texture;
                    completedLoadRequests++;
                }

                void OnFail(int index, string reason)
                {
                    this._textures[index] = null;
                    completedLoadRequests++;
                    Debug.LogError(reason);
                }
            }
            finally
            {
                this._isLoading = false;
            }
        }

        private IEnumerator LoadTextureAtPath(string path, int index, Action<int, Texture2D> onComplete, Action<int, string> onFail)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                onFail?.Invoke(index, "[BatchTextureLoader] File path is empty");
                yield break;
            }

            if (path.TryGetFileUri(out string fileUri) == false)
            {
                onFail?.Invoke(index, $"[BatchTextureLoader] Invalid file URI: {path}");
                yield break;
            }

            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(fileUri, nonReadable: false);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onFail?.Invoke(index, $"[BatchTextureLoader] Failed to load texture from {fileUri}: {request.error}");
                yield break;
            }

            Texture2D texture;

            try
            {
                texture = DownloadHandlerTexture.GetContent(request);

                if (texture != null && string.IsNullOrEmpty(texture.name))
                {
                    texture.name = Path.GetFileNameWithoutExtension(path);
                }
            }
            catch (Exception e)
            {
                onFail?.Invoke(index, $"[BatchTextureLoader] {e.Message}");
                yield break;
            }

            onComplete?.Invoke(index, texture);
        }
    }
}
