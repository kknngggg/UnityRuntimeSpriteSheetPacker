using System;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public interface IFileBrowser
    {
        SelectedFileInfo SelectFile(params string[] extensions);

        void SaveFile(string fileName, ReadOnlySpan<byte> data, string fileExtension = "");

#region NullObject

        private static IFileBrowser s_Null;

        public static IFileBrowser Null => s_Null ??= new NullFileBrowser();

        private class NullFileBrowser : IFileBrowser
        {
            public SelectedFileInfo SelectFile(params string[] extensions)
            {
                Debug.LogError($"[{nameof(IFileBrowser)}] This platform does not have implementation for the {nameof(IFileBrowser)} interface.");
                return SelectedFileInfo.Null;
            }
            public void SaveFile(string fileName, ReadOnlySpan<byte> data, string fileExtension = "")
            {
                Debug.LogError($"[{nameof(IFileBrowser)}] This platform does not have implementation for the {nameof(IFileBrowser)} interface.");
            }
        }

#endregion
    }
}
