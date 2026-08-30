#if UNITY_STANDALONE_OSX && !UNITY_EDITOR

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public sealed class StandaloneMacFileBrowser : IFileBrowser
    {
        private const string LAST_DIR_KEY = "kknngggg.Unity.Sprites.Demos.SpriteSheets.IO.StandaloneMacFileBrowser.LAST_DIR_KEY";

        private static string LastDirectory
        {
            get => PlayerPrefs.GetString(LAST_DIR_KEY, string.Empty);
            set
            {
                PlayerPrefs.SetString(LAST_DIR_KEY, value);
                PlayerPrefs.Save();
            }
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
                Debug.LogError($"[StandaloneMacFileBrowser] Failed to save '{fullSavePath}': {exception.Message}");
            }
        }

        private static string OpenFilePanel(string[] extensions)
        {
            IntPtr ptr = kknngggg_StandaloneMacFileBrowser_OpenFilePanel(
                "Select File",
                LastDirectory ?? string.Empty,
                FileExtension.Join(extensions));

            return ConsumeUtf8(ptr);
        }

        private static bool TryGetFullSavePath(string fileName, string fileExtension, out string fullSavePath)
        {
            string normalizedExtension = FileExtension.Normalize(fileExtension);

            IntPtr ptr = kknngggg_StandaloneMacFileBrowser_SaveFilePanel(
                "Save File",
                LastDirectory ?? string.Empty,
                fileName ?? string.Empty,
                normalizedExtension);

            fullSavePath = EnsureExtension(ConsumeUtf8(ptr), normalizedExtension);

            if (string.IsNullOrWhiteSpace(fullSavePath))
            {
                return false;
            }

            LastDirectory = Path.GetDirectoryName(fullSavePath) ?? string.Empty;
            return true;
        }

        private static string EnsureExtension(string path, string normalizedExtension)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(normalizedExtension))
            {
                return path;
            }

            if (string.IsNullOrEmpty(Path.GetExtension(path)) == false)
            {
                return path;
            }

            return path + "." + normalizedExtension;
        }

        private static string ConsumeUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }

            try
            {
                return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
            }
            finally
            {
                kknngggg_StandaloneMacFileBrowser_Free(ptr);
            }
        }

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr kknngggg_StandaloneMacFileBrowser_OpenFilePanel(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string title,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string directory,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string extensions);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr kknngggg_StandaloneMacFileBrowser_SaveFilePanel(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string title,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string directory,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string defaultName,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string extension);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern void kknngggg_StandaloneMacFileBrowser_Free(IntPtr ptr);
    }
}

#endif
