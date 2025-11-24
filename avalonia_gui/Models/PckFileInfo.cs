namespace WinPCK.Avalonia.Models;

public class PckFileInfo
{
    public string FilePath { get; set; } = "";
    public string Version { get; set; } = "";
    public int VersionId { get; set; }
    public uint FileCount { get; set; }
    public ulong TotalSize { get; set; }
    public ulong DataAreaSize { get; set; }
    public ulong RedundancySize { get; set; }
    public bool SupportsUpdate { get; set; }
    public string AdditionalInfo { get; set; } = "";

    public string TotalSizeMB => $"{TotalSize / 1048576.0:F2} MB";
    public string DataAreaSizeMB => $"{DataAreaSize / 1048576.0:F2} MB";
    public string RedundancySizeMB => $"{RedundancySize / 1048576.0:F2} MB";
}
