#if UNITY_EDITOR_OSX || (UNITY_STANDALONE_OSX && !UNITY_EDITOR)

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace kknngggg.Unity.Sprites.Demos.SpriteSheets.IO
{
    public sealed class MacFileBrowser : IFileBrowser
    {
        private const string LAST_DIR_KEY = "kknngggg.Unity.Sprites.Demos.SpriteSheets.IO.MacFileBrowser.LAST_DIR_KEY";
        private const long NS_MODAL_RESPONSE_OK = 1;
        private const int RTLD_LAZY = 1;

        private static bool s_FrameworksLoaded;

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
            SelectedFileInfo[] files = OpenFiles(extensions, allowMultiple: false);
            return files.Length > 0 ? files[0] : SelectedFileInfo.Null;
        }

        public SelectedFileInfo[] SelectFiles(params string[] extensions)
        {
            return OpenFiles(extensions, allowMultiple: true);
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
                Debug.LogError($"[MacFileBrowser] Failed to save '{fullSavePath}': {exception.Message}");
            }
        }

        private static SelectedFileInfo[] OpenFiles(string[] extensions, bool allowMultiple)
        {
            SelectedFileInfo[] files = RunOpenOnMainThread(() => ShowOpenPanel(extensions, allowMultiple));
            if (files.Length > 0)
            {
                LastDirectory = Path.GetDirectoryName(files[0].FullPath) ?? string.Empty;
            }

            return files;
        }

        private static bool TryGetFullSavePath(string fileName, string fileExtension, out string fullSavePath)
        {
            string normalizedExtension = FileExtension.Normalize(fileExtension);
            fullSavePath = RunSaveOnMainThread(() => ShowSavePanel(
                fileName ?? string.Empty,
                normalizedExtension));
            fullSavePath = EnsureExtension(fullSavePath, normalizedExtension);

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

        private static SelectedFileInfo[] ShowOpenPanel(string[] extensions, bool allowMultiple)
        {
            EnsureFrameworks();

            IntPtr pool = AllocAutoreleasePool();
            try
            {
                IntPtr panel = MsgSend(ObjC.GetClass("NSOpenPanel"), ObjC.Sel("openPanel"));
                if (panel == IntPtr.Zero)
                {
                    Debug.LogError("[MacFileBrowser] Class NSOpenPanel not found");
                    return Array.Empty<SelectedFileInfo>();
                }

                MsgSend(panel, ObjC.Sel("setCanChooseFiles:"), true);
                MsgSend(panel, ObjC.Sel("setCanChooseDirectories:"), false);
                MsgSend(panel, ObjC.Sel("setAllowsMultipleSelection:"), allowMultiple);
                MsgSend(panel, ObjC.Sel("setResolvesAliases:"), true);
                MsgSend(panel, ObjC.Sel("setTitle:"), ToNSString("Select File"));
                SetDirectoryUrl(panel, LastDirectory);
                ApplyAllowedContentTypes(panel, extensions);
                ActivateNsApp();

                if (MsgSend(panel, ObjC.Sel("runModal")).ToInt64() != NS_MODAL_RESPONSE_OK)
                {
                    return Array.Empty<SelectedFileInfo>();
                }

                return ReadSelectedFiles(panel);
            }
            finally
            {
                DrainAutoreleasePool(pool);
            }
        }

        private static string ShowSavePanel(string fileName, string normalizedExtension)
        {
            EnsureFrameworks();

            IntPtr pool = AllocAutoreleasePool();
            try
            {
                IntPtr panel = MsgSend(ObjC.GetClass("NSSavePanel"), ObjC.Sel("savePanel"));
                if (panel == IntPtr.Zero)
                {
                    Debug.LogError("[MacFileBrowser] Class NSSavePanel not found");
                    return string.Empty;
                }

                MsgSend(panel, ObjC.Sel("setCanCreateDirectories:"), true);
                MsgSend(panel, ObjC.Sel("setShowsTagField:"), false);
                MsgSend(panel, ObjC.Sel("setAllowsOtherFileTypes:"), false);
                MsgSend(panel, ObjC.Sel("setTitle:"), ToNSString("Save File"));
                SetDirectoryUrl(panel, LastDirectory);

                if (string.IsNullOrEmpty(fileName) == false)
                {
                    MsgSend(panel, ObjC.Sel("setNameFieldStringValue:"), ToNSString(fileName));
                }

                if (string.IsNullOrEmpty(normalizedExtension) == false)
                {
                    ApplyAllowedContentTypes(panel, new[] { normalizedExtension });
                }

                ActivateNsApp();

                if (MsgSend(panel, ObjC.Sel("runModal")).ToInt64() != NS_MODAL_RESPONSE_OK)
                {
                    return string.Empty;
                }

                IntPtr url = MsgSend(panel, ObjC.Sel("URL"));
                if (url == IntPtr.Zero)
                {
                    return string.Empty;
                }

                return FromNSString(MsgSend(url, ObjC.Sel("path")));
            }
            finally
            {
                DrainAutoreleasePool(pool);
            }
        }

        private static SelectedFileInfo[] ReadSelectedFiles(IntPtr panel)
        {
            IntPtr urls = MsgSend(panel, ObjC.Sel("URLs"));
            if (urls == IntPtr.Zero)
            {
                return Array.Empty<SelectedFileInfo>();
            }

            int count = MsgSend(urls, ObjC.Sel("count")).ToInt32();
            if (count <= 0)
            {
                return Array.Empty<SelectedFileInfo>();
            }

            var files = new List<SelectedFileInfo>(count);
            for (int i = 0; i < count; i++)
            {
                IntPtr url = MsgSend(urls, ObjC.Sel("objectAtIndex:"), new UIntPtr((uint)i));
                if (url == IntPtr.Zero)
                {
                    continue;
                }

                string path = FromNSString(MsgSend(url, ObjC.Sel("path")));
                if (string.IsNullOrWhiteSpace(path) == false)
                {
                    files.Add(new SelectedFileInfo(path));
                }
            }

            return files.Count > 0 ? files.ToArray() : Array.Empty<SelectedFileInfo>();
        }

        private static void ApplyAllowedContentTypes(IntPtr panel, string[] extensions)
        {
            IntPtr types = CreateNormalizedExtensionArray(extensions);
            if (types == IntPtr.Zero)
            {
                return;
            }

            IntPtr utTypeClass = ObjC.GetClass("UTType");
            IntPtr typeWithExt = ObjC.Sel("typeWithFilenameExtension:");
            if (utTypeClass != IntPtr.Zero && MsgSendBool(utTypeClass, ObjC.Sel("respondsToSelector:"), typeWithExt))
            {
                IntPtr contentTypes = MsgSend(ObjC.GetClass("NSMutableArray"), ObjC.Sel("array"));
                int count = MsgSend(types, ObjC.Sel("count")).ToInt32();
                for (int i = 0; i < count; i++)
                {
                    IntPtr ext = MsgSend(types, ObjC.Sel("objectAtIndex:"), new UIntPtr((uint)i));
                    IntPtr utType = MsgSend(utTypeClass, typeWithExt, ext);
                    if (utType != IntPtr.Zero)
                    {
                        MsgSend(contentTypes, ObjC.Sel("addObject:"), utType);
                    }
                }

                if (MsgSend(contentTypes, ObjC.Sel("count")).ToInt32() > 0)
                {
                    MsgSend(panel, ObjC.Sel("setAllowedContentTypes:"), contentTypes);
                    return;
                }
            }

            MsgSend(panel, ObjC.Sel("setAllowedFileTypes:"), types);
        }

        private static IntPtr CreateNormalizedExtensionArray(string[] extensions)
        {
            if (extensions is not { Length: > 0 })
            {
                return IntPtr.Zero;
            }

            IntPtr array = MsgSend(ObjC.GetClass("NSMutableArray"), ObjC.Sel("array"));
            bool added = false;
            for (int i = 0; i < extensions.Length; i++)
            {
                string ext = FileExtension.Normalize(extensions[i]);
                if (ext.Length == 0)
                {
                    continue;
                }

                MsgSend(array, ObjC.Sel("addObject:"), ToNSString(ext));
                added = true;
            }

            return added ? array : IntPtr.Zero;
        }

        private static void SetDirectoryUrl(IntPtr panel, string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            IntPtr url = MsgSend(
                ObjC.GetClass("NSURL"),
                ObjC.Sel("fileURLWithPath:isDirectory:"),
                ToNSString(directory),
                true);
            if (url != IntPtr.Zero)
            {
                MsgSend(panel, ObjC.Sel("setDirectoryURL:"), url);
            }
        }

        private static void ActivateNsApp()
        {
            IntPtr appClass = ObjC.GetClass("NSApplication");
            if (appClass == IntPtr.Zero)
            {
                return;
            }

            IntPtr app = MsgSend(appClass, ObjC.Sel("sharedApplication"));
            if (app == IntPtr.Zero)
            {
                return;
            }

            MsgSend(app, ObjC.Sel("activateIgnoringOtherApps:"), true);
        }

        private static SelectedFileInfo[] RunOpenOnMainThread(Func<SelectedFileInfo[]> work)
        {
            if (IsMainThread())
            {
                return work();
            }

            Debug.LogError("[MacFileBrowser] File panel must run on main thread.");
            return Array.Empty<SelectedFileInfo>();
        }

        private static string RunSaveOnMainThread(Func<string> work)
        {
            if (IsMainThread())
            {
                return work();
            }

            Debug.LogError("[MacFileBrowser] File panel must run on main thread.");
            return string.Empty;
        }

        private static bool IsMainThread()
        {
            IntPtr nsThread = ObjC.GetClass("NSThread");
            return nsThread != IntPtr.Zero && MsgSendByte(nsThread, ObjC.Sel("isMainThread")) != 0;
        }

        private static void EnsureFrameworks()
        {
            if (s_FrameworksLoaded)
            {
                return;
            }

            Dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", RTLD_LAZY);
            Dlopen(
                "/System/Library/Frameworks/UniformTypeIdentifiers.framework/UniformTypeIdentifiers",
                RTLD_LAZY);
            s_FrameworksLoaded = true;
        }

        private static IntPtr AllocAutoreleasePool()
        {
            return MsgSend(MsgSend(ObjC.GetClass("NSAutoreleasePool"), ObjC.Sel("alloc")), ObjC.Sel("init"));
        }

        private static void DrainAutoreleasePool(IntPtr pool)
        {
            if (pool != IntPtr.Zero)
            {
                MsgSend(pool, ObjC.Sel("drain"));
            }
        }

        private static IntPtr ToNSString(string value)
        {
            if (value == null)
            {
                return IntPtr.Zero;
            }

            return MsgSend(ObjC.GetClass("NSString"), ObjC.Sel("stringWithUTF8String:"), value);
        }

        private static string FromNSString(IntPtr nsString)
        {
            if (nsString == IntPtr.Zero)
            {
                return string.Empty;
            }

            return Marshal.PtrToStringUTF8(MsgSend(nsString, ObjC.Sel("UTF8String"))) ?? string.Empty;
        }

        private static class ObjC
        {
            public static IntPtr GetClass(string name) => objc_getClass(name);

            public static IntPtr Sel(string name) => sel_registerName(name);
        }

        [DllImport("/usr/lib/libobjc.A.dylib")]
        private static extern IntPtr objc_getClass(string name);

        [DllImport("/usr/lib/libobjc.A.dylib")]
        private static extern IntPtr sel_registerName(string name);

        [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "dlopen")]
        private static extern IntPtr Dlopen(string path, int mode);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr MsgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr MsgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr MsgSend(
            IntPtr receiver,
            IntPtr selector,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string arg);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr MsgSend(
            IntPtr receiver,
            IntPtr selector,
            [MarshalAs(UnmanagedType.I1)] bool arg);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr MsgSend(
            IntPtr receiver,
            IntPtr selector,
            IntPtr arg1,
            [MarshalAs(UnmanagedType.I1)] bool arg2);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr MsgSend(IntPtr receiver, IntPtr selector, UIntPtr arg);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool MsgSendBool(IntPtr receiver, IntPtr selector, IntPtr arg);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern byte MsgSendByte(IntPtr receiver, IntPtr selector);
    }
}

#endif
