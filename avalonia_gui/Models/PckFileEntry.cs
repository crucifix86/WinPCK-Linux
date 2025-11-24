using System;
using WinPCK.Avalonia.Native;

namespace WinPCK.Avalonia.Models;

public class PckFileEntry
{
    public string Name { get; set; } = "";
    public int EntryType { get; set; }
    public ulong Size { get; set; }
    public ulong CompressedSize { get; set; }
    public IntPtr EntryPointer { get; set; }

    public bool IsFolder => (EntryType & PckNative.PCK_ENTRY_TYPE_FOLDER) != 0;
    public bool IsFile => (EntryType & PckNative.PCK_ENTRY_TYPE_INDEX) != 0;

    public string TypeIcon => IsFolder ? "📁" : "📄";
    public string SizeFormatted => Size > 0 ? FormatSize(Size) : "";
    public string CompressedSizeFormatted => CompressedSize > 0 ? FormatSize(CompressedSize) : "";

    public string CompressionRatio
    {
        get
        {
            if (CompressedSize > 0 && Size > 0)
            {
                var ratio = (double)CompressedSize / Size * 100;
                return $"{ratio:F1}%";
            }
            return "-";
        }
    }

    private static string FormatSize(ulong size)
    {
        if (size < 1024)
            return $"{size} B";
        if (size < 1024 * 1024)
            return $"{size / 1024.0:F2} KB";
        if (size < 1024 * 1024 * 1024)
            return $"{size / (1024.0 * 1024):F2} MB";
        return $"{size / (1024.0 * 1024 * 1024):F2} GB";
    }
}
