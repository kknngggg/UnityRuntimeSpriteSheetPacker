using System;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public interface IFileBrowser
    {
        SelectedFileInfo SelectFile(params string[] extensions);

        void SaveFile(string fileName, ReadOnlySpan<byte> data, string fileExtension = "");
    }
}
