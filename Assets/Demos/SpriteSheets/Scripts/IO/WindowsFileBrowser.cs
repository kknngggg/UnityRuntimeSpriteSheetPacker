#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public sealed class WindowsFileBrowser : IFileBrowser
    {
        private const string LAST_DIR_KEY = "kknngggg.Unity.Sprites.Demos.SpriteSheets.IO.WindowsFileBrowser.LAST_DIR_KEY";
        private const int FILE_PATH_BUFFER_CHARS = 32768;
        private const int OFN_OVERWRITEPROMPT = 0x00000002;
        private const int OFN_HIDEREADONLY = 0x00000004;
        private const int OFN_NOCHANGEDIR = 0x00000008;
        private const int OFN_ALLOWMULTISELECT = 0x00000200;
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_EXPLORER = 0x00080000;

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
            SelectedFileInfo[] files = OpenFiles(extensions, allowMultiSelect: false);
            return files.Length > 0 ? files[0] : SelectedFileInfo.Null;
        }

        public SelectedFileInfo[] SelectFiles(params string[] extensions)
        {
            return OpenFiles(extensions, allowMultiSelect: true);
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
                Debug.LogError($"[WindowsFileBrowser] Failed to save '{fullSavePath}': {exception.Message}");
            }
        }

        private static SelectedFileInfo[] OpenFiles(string[] extensions, bool allowMultiSelect)
        {
            SelectedFileInfo[] files = ShowDialog(save: false,
                                                  "Select File",
                                                  LastDirectory,
                                                  defaultName: string.Empty,
                                                  extensions,
                                                  defaultExtension: string.Empty,
                                                  allowMultiSelect);

            if (files.Length > 0)
            {
                LastDirectory = Path.GetDirectoryName(files[0].FullPath) ?? string.Empty;
            }

            return files;
        }

        private static bool TryGetFullSavePath(string fileName, string fileExtension, out string fullSavePath)
        {
            string[] extensions = string.IsNullOrWhiteSpace(fileExtension)
                ? Array.Empty<string>()
                : new[] { fileExtension };

            SelectedFileInfo[] files = ShowDialog(save: true,
                                                  "Save File",
                                                  LastDirectory,
                                                  fileName ?? string.Empty,
                                                  extensions,
                                                  fileExtension ?? string.Empty,
                                                  allowMultiSelect: false);

            if (files.Length == 0)
            {
                fullSavePath = string.Empty;
                return false;
            }

            fullSavePath = files[0].FullPath;
            LastDirectory = Path.GetDirectoryName(fullSavePath) ?? string.Empty;
            return true;
        }

        private static SelectedFileInfo[] ShowDialog(
            bool save,
            string title,
            string directory,
            string defaultName,
            string[] extensions,
            string defaultExtension,
            bool allowMultiSelect)
        {
            IntPtr filterPtr = IntPtr.Zero;
            IntPtr filePtr = IntPtr.Zero;
            IntPtr dirPtr = IntPtr.Zero;
            IntPtr titlePtr = IntPtr.Zero;
            IntPtr defExtPtr = IntPtr.Zero;

            try
            {
                filterPtr = Marshal.StringToHGlobalUni(BuildFilter(extensions));
                filePtr = AllocFileBuffer(save ? defaultName : string.Empty);
                dirPtr = string.IsNullOrWhiteSpace(directory)
                    ? IntPtr.Zero
                    : Marshal.StringToHGlobalUni(directory);
                titlePtr = Marshal.StringToHGlobalUni(title ?? string.Empty);

                string defExt = FileExtension.Normalize(defaultExtension);
                defExtPtr = string.IsNullOrEmpty(defExt)
                    ? IntPtr.Zero
                    : Marshal.StringToHGlobalUni(defExt);

                var ofn = new OpenFileName
                {
                    structSize = Marshal.SizeOf<OpenFileName>(),
                    dlgOwner = GetActiveWindow(),
                    filter = filterPtr,
                    filterIndex = 1,
                    file = filePtr,
                    maxFile = FILE_PATH_BUFFER_CHARS,
                    initialDir = dirPtr,
                    title = titlePtr,
                    defExt = defExtPtr,
                    flags = OFN_EXPLORER | OFN_HIDEREADONLY | OFN_NOCHANGEDIR
                };

                if (save)
                {
                    ofn.flags |= OFN_OVERWRITEPROMPT | OFN_PATHMUSTEXIST;
                }
                else
                {
                    ofn.flags |= OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST;
                    if (allowMultiSelect)
                    {
                        ofn.flags |= OFN_ALLOWMULTISELECT;
                    }
                }

                bool ok = save ? GetSaveFileName(ref ofn) : GetOpenFileName(ref ofn);
                if (ok)
                {
                    return ReadSelectedFiles(filePtr, allowMultiSelect);
                }

                int error = CommDlgExtendedError();
                if (error != 0)
                {
                    Debug.LogError($"[WindowsFileBrowser] Native file dialog failed. CommDlgExtendedError=0x{error:X8}");
                }

                return Array.Empty<SelectedFileInfo>();
            }
            finally
            {
                FreeHGlobal(filterPtr);
                FreeHGlobal(filePtr);
                FreeHGlobal(dirPtr);
                FreeHGlobal(titlePtr);
                FreeHGlobal(defExtPtr);
            }
        }

        private static SelectedFileInfo[] ReadSelectedFiles(IntPtr filePtr, bool allowMultiSelect)
        {
            if (allowMultiSelect == false)
            {
                string path = Marshal.PtrToStringUni(filePtr) ?? string.Empty;
                return string.IsNullOrWhiteSpace(path)
                    ? Array.Empty<SelectedFileInfo>()
                    : new[] { new SelectedFileInfo(path) };
            }

            var parts = new List<string>();
            IntPtr cursor = filePtr;
            while (true)
            {
                string part = Marshal.PtrToStringUni(cursor);
                if (string.IsNullOrEmpty(part))
                {
                    break;
                }

                parts.Add(part);
                cursor = IntPtr.Add(cursor, (part.Length + 1) * sizeof(char));
            }

            if (parts.Count == 0)
            {
                return Array.Empty<SelectedFileInfo>();
            }

            if (parts.Count == 1)
            {
                return new[] { new SelectedFileInfo(parts[0]) };
            }

            string directory = parts[0];
            var files = new SelectedFileInfo[parts.Count - 1];
            for (int i = 1; i < parts.Count; i++)
            {
                files[i - 1] = new SelectedFileInfo(Path.Combine(directory, parts[i]));
            }

            return files;
        }

        private static void FreeHGlobal(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private static IntPtr AllocFileBuffer(string initialName)
        {
            int byteCount = FILE_PATH_BUFFER_CHARS * 2;
            IntPtr buffer = Marshal.AllocHGlobal(byteCount);
            byte[] zeros = new byte[byteCount];
            Marshal.Copy(zeros, 0, buffer, byteCount);

            if (string.IsNullOrEmpty(initialName) == false)
            {
                byte[] nameBytes = Encoding.Unicode.GetBytes(initialName);
                int copyCount = Math.Min(nameBytes.Length, byteCount - 2);
                Marshal.Copy(nameBytes, 0, buffer, copyCount);
            }

            return buffer;
        }

        private static string BuildFilter(string[] extensions)
        {
            if (extensions is not { Length: > 0 })
            {
                return "All Files\0*.*\0";
            }

            var patterns = new StringBuilder("Images\0");
            for (int i = 0; i < extensions.Length; i++)
            {
                if (i > 0)
                {
                    patterns.Append(';');
                }

                patterns.Append("*.");
                patterns.Append(FileExtension.Normalize(extensions[i]));
            }

            patterns.Append('\0');
            return patterns.ToString();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOpenFileName(ref OpenFileName ofn);

        [DllImport("comdlg32.dll", EntryPoint = "GetSaveFileNameW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSaveFileName(ref OpenFileName ofn);

        [DllImport("comdlg32.dll")]
        private static extern int CommDlgExtendedError();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenFileName
        {
            public int structSize;
            public IntPtr dlgOwner;
            public IntPtr instance;
            public IntPtr filter;
            public IntPtr customFilter;
            public int maxCustFilter;
            public int filterIndex;
            public IntPtr file;
            public int maxFile;
            public IntPtr fileTitle;
            public int maxFileTitle;
            public IntPtr initialDir;
            public IntPtr title;
            public int flags;
            public short fileOffset;
            public short fileExtension;
            public IntPtr defExt;
            public IntPtr custData;
            public IntPtr hook;
            public IntPtr templateName;
            public IntPtr reservedPtr;
            public int reservedInt;
            public int flagsEx;
        }
    }
}

#endif
