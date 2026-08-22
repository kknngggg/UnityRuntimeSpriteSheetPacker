#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public sealed class UnityEditorFileBrowser : IFileBrowser
    {
        private const string LAST_DIR_KEY = "kknngggg.Unity.Sprites.Demos.SpriteSheets.IO.UnityEditorFileBrowser.LAST_DIR_KEY";

        private static string LastDirectory {
            get => EditorPrefs.GetString(LAST_DIR_KEY, string.Empty);
            set => EditorPrefs.SetString(LAST_DIR_KEY, value);
        }

        public SelectedFileInfo SelectFile(params string[] extensions)
        {
            string fullPath = OpenFilePanel(extensions);

            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return SelectedFileInfo.Null;
            }

            LastDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty;
            return new SelectedFileInfo(fullPath);
        }

        public void SaveFile(string fileName, ReadOnlySpan<byte> data, string fileExtension = "")
        {
            if (TryGetFullSavePath(fileName, fileExtension, out string fullSavePath) == false)
            {
                return;
            }

            try
            {
                using FileStream fs = new FileStream(fullSavePath, FileMode.Create);
                fs.Write(data);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[UnityEditorFileBrowser] Failed to save '{fullSavePath}': {exception.Message}");
            }
        }

        private static string OpenFilePanel(string[] extensions)
        {
            if (extensions is { Length: > 0 })
            {
                return EditorUtility.OpenFilePanelWithFilters(
                    "Select File",
                    LastDirectory,
                    new[] { "Images", string.Join(",", extensions) });
            }

            return EditorUtility.OpenFilePanel("Select File", LastDirectory, string.Empty);
        }

        private static bool TryGetFullSavePath(string fileName, string fileExtension, out string fullSavePath)
        {
            fullSavePath = EditorUtility.SaveFilePanel("Save File",
                                                       LastDirectory,
                                                       fileName,
                                                       fileExtension);

            if (string.IsNullOrWhiteSpace(fullSavePath))
            {
                return false;
            }

            LastDirectory = Path.GetDirectoryName(fullSavePath) ?? string.Empty;
            return true;
        }
    }
}

#endif
