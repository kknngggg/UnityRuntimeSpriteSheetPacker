using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public sealed class BatchTextureLoader
    {
        private readonly IEnumerable<string> _diskPaths;
        private readonly List<Texture2D> _textures;

        public BatchTextureLoader(IEnumerable<string> diskPaths, int count)
        {
            this._diskPaths = diskPaths;
            this._textures = new List<Texture2D>(count);
        }

        public IReadOnlyList<Texture2D> Textures => this._textures;

        public IEnumerator LoadAllAsync()
        {
            yield break; // TODO: complete me please UwU
        }
    }
}
