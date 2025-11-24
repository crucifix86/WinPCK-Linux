using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WinPCK.Avalonia.Native;

/// <summary>
/// P/Invoke wrapper for the native PCK library
/// </summary>
public static class PckNative
{
    private const string LibName = "pcklib";

    #region Enums and Constants

    public enum PCKRTN : int
    {
        WINPCK_OK = 0,
        WINPCK_ERROR = -1
    }

    public const int PCK_ENTRY_TYPE_INDEX = 1;
    public const int PCK_ENTRY_TYPE_FOLDER = 2;
    public const int PCK_ENTRY_TYPE_DOTDOT = 4;

    #endregion

    #region Delegates

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LogCallback(byte level, IntPtr message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ShowListCallback(
        IntPtr param,
        int serialNumber,
        IntPtr fileName,  // Changed to IntPtr - will manually convert from UTF-32
        int entryType,
        ulong fileSize,
        ulong fileSizeCompressed,
        IntPtr fileEntry);

    #endregion

    #region Helper Methods

    // Convert UTF-32 wchar_t* (Linux) to C# string
    public static string Utf32PtrToString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;

        var chars = new List<char>();
        int offset = 0;

        while (true)
        {
            // Read 4 bytes (UTF-32 character)
            int utf32Char = Marshal.ReadInt32(ptr, offset);

            if (utf32Char == 0)
                break;

            // Convert UTF-32 to UTF-16 (C# char)
            if (utf32Char <= 0xFFFF)
            {
                chars.Add((char)utf32Char);
            }
            else
            {
                // Surrogate pair for characters > U+FFFF
                utf32Char -= 0x10000;
                chars.Add((char)((utf32Char >> 10) + 0xD800));
                chars.Add((char)((utf32Char & 0x3FF) + 0xDC00));
            }

            offset += 4;
        }

        return new string(chars.ToArray());
    }

    // Convert C# string to UTF-32 wchar_t* (Linux) - caller must free
    public static IntPtr StringToUtf32Ptr(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            IntPtr empty = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(empty, 0, 0);
            return empty;
        }

        // Convert string to UTF-32 code points
        var utf32 = new List<int>();
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (char.IsHighSurrogate(c) && i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
            {
                // Surrogate pair
                int high = c - 0xD800;
                int low = str[i + 1] - 0xDC00;
                utf32.Add(0x10000 + (high << 10) + low);
                i++; // Skip low surrogate
            }
            else
            {
                utf32.Add(c);
            }
        }
        utf32.Add(0); // Null terminator

        // Allocate and copy
        IntPtr ptr = Marshal.AllocHGlobal(utf32.Count * 4);
        for (int i = 0; i < utf32.Count; i++)
        {
            Marshal.WriteInt32(ptr, i * 4, utf32[i]);
        }

        return ptr;
    }

    // Free UTF-32 string allocated by StringToUtf32Ptr
    public static void FreeUtf32Ptr(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero)
            Marshal.FreeHGlobal(ptr);
    }

    #endregion

    #region File Operations

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pck_open")]
    private static extern PCKRTN pck_open_native(IntPtr filename);

    public static PCKRTN pck_open(string filename)
    {
        IntPtr utf32Ptr = MarshalStringToUTF32(filename);
        try
        {
            // Debug: verify the UTF-32 encoding
            Console.WriteLine($"[DEBUG] Original: {filename} (len={filename.Length})");
            Console.Write("[DEBUG] UTF-32 bytes: ");
            for (int i = 0; i <= filename.Length && i < 10; i++) {
                int val = Marshal.ReadInt32(utf32Ptr, i * 4);
                Console.Write($"{val:X8} ");
            }
            Console.WriteLine();

            return pck_open_native(utf32Ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(utf32Ptr);
        }
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pck_close();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool pck_IsValidPck();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pck_filecount();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong pck_filesize();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong pck_file_data_area_size();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong pck_file_redundancy_data_size();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int pck_getVersion();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public static extern IntPtr pck_GetCurrentVersionName();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool pck_isSupportAddFileToPck();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr pck_GetAdditionalInfo();

    #endregion

    #region Navigation

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr pck_getRootNode();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    public static extern IntPtr pck_getFileEntryByPath([MarshalAs(UnmanagedType.LPWStr)] string path);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pck_listByNode(
        IntPtr entry,
        IntPtr param,
        ShowListCallback callback);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pck_searchByName")]
    private static extern uint pck_searchByName_native(
        IntPtr searchString,
        IntPtr param,
        ShowListCallback callback);

    public static uint pck_searchByName(string searchString, IntPtr param, ShowListCallback callback)
    {
        IntPtr ptr = StringToUtf32Ptr(searchString);
        try
        {
            return pck_searchByName_native(ptr, param, callback);
        }
        finally
        {
            FreeUtf32Ptr(ptr);
        }
    }

    #endregion

    #region Extraction

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pck_ExtractAllFiles")]
    private static extern PCKRTN pck_ExtractAllFiles_native(IntPtr destDir);

    public static PCKRTN pck_ExtractAllFiles(string destDir)
    {
        IntPtr ptr = StringToUtf32Ptr(destDir);
        try
        {
            return pck_ExtractAllFiles_native(ptr);
        }
        finally
        {
            FreeUtf32Ptr(ptr);
        }
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pck_ExtractFilesByEntrys")]
    private static extern PCKRTN pck_ExtractFilesByEntrys_native(
        IntPtr[] fileEntries,
        int count,
        IntPtr destDir);

    public static PCKRTN pck_ExtractFiles(IntPtr[] fileEntries, int count, string destDir)
    {
        IntPtr ptr = StringToUtf32Ptr(destDir);
        try
        {
            return pck_ExtractFilesByEntrys_native(fileEntries, count, ptr);
        }
        finally
        {
            FreeUtf32Ptr(ptr);
        }
    }

    // Extract all files from a PCK without opening it first
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "do_ExtractAllFiles")]
    private static extern PCKRTN do_ExtractAllFiles_native(IntPtr srcPckFile, IntPtr destDir);

    public static PCKRTN do_ExtractAllFiles(string srcPckFile, string destDir)
    {
        IntPtr srcPtr = StringToUtf32Ptr(srcPckFile);
        IntPtr dstPtr = StringToUtf32Ptr(destDir);
        try
        {
            return do_ExtractAllFiles_native(srcPtr, dstPtr);
        }
        finally
        {
            FreeUtf32Ptr(srcPtr);
            FreeUtf32Ptr(dstPtr);
        }
    }

    #endregion

    #region Progress

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool pck_isThreadWorking();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool pck_isLastOptSuccess();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pck_getUIProgress();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pck_getUIProgressUpper();

    #endregion

    #region Creation/Editing

    // Create new PCK file
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "do_CreatePckFile")]
    private static extern PCKRTN do_CreatePckFile_native(IntPtr srcDir, IntPtr pckFile, int version, int compressionLevel);

    public static PCKRTN do_CreatePckFile(string srcDir, string pckFile, int version, int compressionLevel)
    {
        IntPtr srcPtr = StringToUtf32Ptr(srcDir);
        IntPtr dstPtr = StringToUtf32Ptr(pckFile);
        try { return do_CreatePckFile_native(srcPtr, dstPtr, version, compressionLevel); }
        finally { FreeUtf32Ptr(srcPtr); FreeUtf32Ptr(dstPtr); }
    }

    // Add files to PCK
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "do_AddFileToPckFile")]
    private static extern PCKRTN do_AddFileToPckFile_native(IntPtr srcPath, IntPtr pckFile, IntPtr pathInPck, int level);

    public static PCKRTN do_AddFileToPckFile(string srcPath, string pckFile, string pathInPck, int level)
    {
        IntPtr srcPtr = StringToUtf32Ptr(srcPath);
        IntPtr dstPtr = StringToUtf32Ptr(pckFile);
        IntPtr pathPtr = StringToUtf32Ptr(pathInPck);
        try { return do_AddFileToPckFile_native(srcPtr, dstPtr, pathPtr, level); }
        finally { FreeUtf32Ptr(srcPtr); FreeUtf32Ptr(dstPtr); FreeUtf32Ptr(pathPtr); }
    }

    // Rename file in PCK
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pck_RenameEntry")]
    private static extern PCKRTN pck_RenameEntry_native(IntPtr fileEntry, IntPtr newName);

    public static PCKRTN pck_RenameEntry(IntPtr fileEntry, string newName)
    {
        IntPtr namePtr = StringToUtf32Ptr(newName);
        try { return pck_RenameEntry_native(fileEntry, namePtr); }
        finally { FreeUtf32Ptr(namePtr); }
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PCKRTN pck_RenameSubmit();

    // Delete file from PCK
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PCKRTN pck_DeleteEntry(IntPtr fileEntry);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern PCKRTN pck_DeleteEntrySubmit();

    // Rebuild PCK
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "pck_RebuildPckFile")]
    private static extern PCKRTN pck_RebuildPckFile_native(IntPtr outputPath, [MarshalAs(UnmanagedType.Bool)] bool useRecompress);

    public static PCKRTN pck_RebuildPckFile(string outputPath, bool useRecompress)
    {
        IntPtr ptr = StringToUtf32Ptr(outputPath);
        try { return pck_RebuildPckFile_native(ptr, useRecompress); }
        finally { FreeUtf32Ptr(ptr); }
    }

    #endregion

    #region Logging

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void log_regShowFunc(LogCallback callback);

    #endregion

    #region Helper Methods

    /// <summary>
    /// Marshal a UTF-32 wchar_t* pointer to a .NET string (Linux wchar_t is 4 bytes)
    /// </summary>
    public static string MarshalUTF32String(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;

        // Read UTF-32 characters until null terminator
        var chars = new System.Collections.Generic.List<int>();
        int offset = 0;
        while (true)
        {
            int codePoint = Marshal.ReadInt32(ptr, offset);
            if (codePoint == 0)
                break;
            chars.Add(codePoint);
            offset += 4;
        }

        // Convert UTF-32 code points to .NET string
        return string.Concat(chars.Select(cp => char.ConvertFromUtf32(cp)));
    }

    /// <summary>
    /// Marshal a .NET string to UTF-32 wchar_t* (Linux wchar_t is 4 bytes)
    /// Caller must free the returned pointer with Marshal.FreeHGlobal
    /// </summary>
    public static IntPtr MarshalStringToUTF32(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            IntPtr nullPtr = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(nullPtr, 0);
            return nullPtr;
        }

        // Convert string to UTF-32 code points
        var codePoints = new System.Collections.Generic.List<int>();
        for (int i = 0; i < str.Length; i++)
        {
            int codePoint = char.ConvertToUtf32(str, i);
            codePoints.Add(codePoint);

            // Skip the low surrogate if we consumed a surrogate pair
            if (char.IsHighSurrogate(str[i]))
                i++;
        }

        // Allocate memory for UTF-32 string (4 bytes per character + 4 for null terminator)
        IntPtr ptr = Marshal.AllocHGlobal((codePoints.Count + 1) * 4);

        // Write each code point as 4-byte integer
        for (int i = 0; i < codePoints.Count; i++)
        {
            Marshal.WriteInt32(ptr, i * 4, codePoints[i]);
        }

        // Write null terminator
        Marshal.WriteInt32(ptr, codePoints.Count * 4, 0);

        return ptr;
    }

    #endregion
}
