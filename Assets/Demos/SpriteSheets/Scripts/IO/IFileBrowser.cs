using System;
using System.IO;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public interface IFileBrowser
    {
        FileSystemInfo SelectFile(params string[] extensions);

        void SaveFile(string path, ReadOnlySpan<byte> data);
    }
}
