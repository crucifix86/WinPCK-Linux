using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Serilog;
using WinPCK.Avalonia.Models;
using WinPCK.Avalonia.Native;

namespace WinPCK.Avalonia.Services;

/// <summary>
/// Managed wrapper for PCK file operations with logging
/// </summary>
public class PckService : IDisposable
{
    private readonly ILogger _logger;
    private bool _isFileOpen;
    private string? _currentFilePath;
    private PckNative.LogCallback? _logCallback;

    public bool IsFileOpen => _isFileOpen;
    public string? CurrentFilePath => _currentFilePath;

    public event EventHandler<string>? LogMessageReceived;
    public event EventHandler<ProgressEventArgs>? ProgressChanged;

    public PckService(ILogger logger)
    {
        _logger = logger;

        // Register native log callback
        _logCallback = OnNativeLogMessage;
        PckNative.log_regShowFunc(_logCallback);

        _logger.Information("PckService initialized");
    }

    private void OnNativeLogMessage(byte level, IntPtr messagePtr)
    {
        // Marshal UTF-32 string (Linux wchar_t is 4 bytes, not 2 like Windows)
        var message = PckNative.MarshalUTF32String(messagePtr);

        var logLevel = level switch
        {
            (byte)'E' => "ERROR",
            (byte)'W' => "WARNING",
            (byte)'I' => "INFO",
            (byte)'D' => "DEBUG",
            _ => "UNKNOWN"
        };

        _logger.Information("[Native {Level}] {Message}", logLevel, message);
        LogMessageReceived?.Invoke(this, $"[{logLevel}] {message}");
    }

    public bool OpenFile(string filePath)
    {
        try
        {
            _logger.Information("Opening PCK file: {FilePath}", filePath);
            _logger.Debug("File path length: {Length} characters", filePath.Length);

            var result = PckNative.pck_open(filePath);

            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Failed to open PCK file: {Result}", result);
                return false;
            }

            if (!PckNative.pck_IsValidPck())
            {
                _logger.Error("Invalid PCK file format");
                PckNative.pck_close();
                return false;
            }

            _isFileOpen = true;
            _currentFilePath = filePath;

            var fileCount = PckNative.pck_filecount();
            var fileSize = PckNative.pck_filesize();
            var versionPtr = PckNative.pck_GetCurrentVersionName();
            var version = Marshal.PtrToStringUni(versionPtr) ?? "Unknown";

            _logger.Information("PCK file opened successfully: Version={Version}, Files={FileCount}, Size={FileSize:N0} bytes",
                version, fileCount, fileSize);

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while opening PCK file");
            return false;
        }
    }

    public void CloseFile()
    {
        if (_isFileOpen)
        {
            _logger.Information("Closing PCK file: {FilePath}", _currentFilePath);
            PckNative.pck_close();
            _isFileOpen = false;
            _currentFilePath = null;
        }
    }

    public PckFileInfo? GetFileInfo()
    {
        if (!_isFileOpen)
        {
            _logger.Warning("Attempted to get file info without an open file");
            return null;
        }

        try
        {
            var versionPtr = PckNative.pck_GetCurrentVersionName();
            var version = Marshal.PtrToStringUni(versionPtr) ?? "Unknown";

            var additionalInfoPtr = PckNative.pck_GetAdditionalInfo();
            var additionalInfo = additionalInfoPtr != IntPtr.Zero
                ? Marshal.PtrToStringAnsi(additionalInfoPtr) ?? ""
                : "";

            var info = new PckFileInfo
            {
                FilePath = _currentFilePath ?? "",
                Version = version,
                VersionId = PckNative.pck_getVersion(),
                FileCount = PckNative.pck_filecount(),
                TotalSize = PckNative.pck_filesize(),
                DataAreaSize = PckNative.pck_file_data_area_size(),
                RedundancySize = PckNative.pck_file_redundancy_data_size(),
                SupportsUpdate = PckNative.pck_isSupportAddFileToPck(),
                AdditionalInfo = additionalInfo
            };

            _logger.Debug("Retrieved file info: {@FileInfo}", info);
            return info;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting file info");
            return null;
        }
    }

    public List<PckFileEntry> ListFiles(IntPtr? entry = null)
    {
        if (!_isFileOpen)
        {
            _logger.Warning("Attempted to list files without an open file");
            return new List<PckFileEntry>();
        }

        var entries = new List<PckFileEntry>();

        try
        {
            var entryPtr = entry ?? PckNative.pck_getRootNode();

            PckNative.ShowListCallback callback = (param, sn, fileNamePtr, entryType, fileSize, fileSizeCompressed, fileEntry) =>
            {
                var fileName = PckNative.Utf32PtrToString(fileNamePtr);
                entries.Add(new PckFileEntry
                {
                    Name = fileName,
                    EntryType = entryType,
                    Size = fileSize,
                    CompressedSize = fileSizeCompressed,
                    EntryPointer = fileEntry
                });
            };

            var count = PckNative.pck_listByNode(entryPtr, IntPtr.Zero, callback);
            _logger.Debug("Listed {Count} entries", count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error listing files");
        }

        return entries;
    }

    public bool ExtractSelectedFiles(IntPtr[] fileEntries, int count, string destDir)
    {
        if (!_isFileOpen)
        {
            _logger.Warning("Attempted to extract files without an open file");
            return false;
        }

        try
        {
            _logger.Information("Extracting {Count} selected files to: {DestDir}", count, destDir);

            var result = PckNative.pck_ExtractFiles(fileEntries, count, destDir);

            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Extraction failed: {Result}", result);
                return false;
            }

            // Monitor progress
            while (PckNative.pck_isThreadWorking())
            {
                var progress = PckNative.pck_getUIProgress();
                var total = PckNative.pck_getUIProgressUpper();

                ProgressChanged?.Invoke(this, new ProgressEventArgs(progress, total));

                System.Threading.Thread.Sleep(100);
            }

            var success = PckNative.pck_isLastOptSuccess();

            if (success)
            {
                _logger.Information("Selected files extraction completed successfully");
            }
            else
            {
                _logger.Error("Selected files extraction failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception during extraction");
            return false;
        }
    }

    public List<PckFileEntry> SearchFiles(string searchString)
    {
        if (!_isFileOpen)
        {
            _logger.Warning("Attempted to search files without an open file");
            return new List<PckFileEntry>();
        }

        var results = new List<PckFileEntry>();

        try
        {
            _logger.Information("Searching for: {SearchString}", searchString);

            PckNative.ShowListCallback callback = (param, sn, fileNamePtr, entryType, fileSize, fileSizeCompressed, fileEntry) =>
            {
                var fileName = PckNative.Utf32PtrToString(fileNamePtr);
                results.Add(new PckFileEntry
                {
                    Name = fileName,
                    EntryType = entryType,
                    Size = fileSize,
                    CompressedSize = fileSizeCompressed,
                    EntryPointer = fileEntry
                });
            };

            var count = PckNative.pck_searchByName(searchString, IntPtr.Zero, callback);
            _logger.Information("Search found {Count} results", count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error searching files");
        }

        return results;
    }

    public bool ExtractAllFiles(string destDir)
    {
        if (!_isFileOpen)
        {
            _logger.Warning("Attempted to extract files without an open file");
            return false;
        }

        try
        {
            _logger.Information("Extracting all files to: {DestDir}", destDir);

            var result = PckNative.pck_ExtractAllFiles(destDir);

            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Extraction failed: {Result}", result);
                return false;
            }

            // Monitor progress
            while (PckNative.pck_isThreadWorking())
            {
                var progress = PckNative.pck_getUIProgress();
                var total = PckNative.pck_getUIProgressUpper();

                ProgressChanged?.Invoke(this, new ProgressEventArgs(progress, total));

                System.Threading.Thread.Sleep(100);
            }

            var success = PckNative.pck_isLastOptSuccess();

            if (success)
            {
                _logger.Information("Extraction completed successfully");
            }
            else
            {
                _logger.Error("Extraction completed with errors");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception during extraction");
            return false;
        }
    }

    public bool CreateNewPck(string srcPath, string dstPckFile, int compressionLevel = 9)
    {
        try
        {
            _logger.Information("Creating new PCK: {DstPck} from {SrcPath}", dstPckFile, srcPath);

            var result = PckNative.do_CreatePckFile(srcPath, dstPckFile, 0, compressionLevel);

            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Failed to create PCK file: {Result}", result);
                return false;
            }

            // Wait for operation to complete
            while (PckNative.pck_isThreadWorking())
            {
                var progress = PckNative.pck_getUIProgress();
                var total = PckNative.pck_getUIProgressUpper();
                ProgressChanged?.Invoke(this, new ProgressEventArgs(progress, total));
                System.Threading.Thread.Sleep(100);
            }

            var success = PckNative.pck_isLastOptSuccess();
            _logger.Information("PCK creation {Status}", success ? "succeeded" : "failed");
            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception during PCK creation");
            return false;
        }
    }

    public bool AddFilesToPck(string srcPath, string pckFile, string pathInPck, int compressionLevel = 9)
    {
        try
        {
            _logger.Information("Adding files to PCK: {PckFile}", pckFile);

            var result = PckNative.do_AddFileToPckFile(srcPath, pckFile, pathInPck, compressionLevel);

            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Failed to add files to PCK: {Result}", result);
                return false;
            }

            // Wait for operation to complete
            while (PckNative.pck_isThreadWorking())
            {
                var progress = PckNative.pck_getUIProgress();
                var total = PckNative.pck_getUIProgressUpper();
                ProgressChanged?.Invoke(this, new ProgressEventArgs(progress, total));
                System.Threading.Thread.Sleep(100);
            }

            var success = PckNative.pck_isLastOptSuccess();
            _logger.Information("Add files {Status}", success ? "succeeded" : "failed");
            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception adding files to PCK");
            return false;
        }
    }

    public bool RenameFile(IntPtr fileEntry, string newName)
    {
        if (!_isFileOpen)
        {
            _logger.Warning("Attempted to rename file without an open PCK");
            return false;
        }

        try
        {
            _logger.Information("Renaming file to: {NewName}", newName);

            var result = PckNative.pck_RenameEntry(fileEntry, newName);
            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Failed to rename file: {Result}", result);
                return false;
            }

            result = PckNative.pck_RenameSubmit();
            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Failed to submit rename: {Result}", result);
                return false;
            }

            // Wait for operation to complete
            while (PckNative.pck_isThreadWorking())
            {
                System.Threading.Thread.Sleep(100);
            }

            var success = PckNative.pck_isLastOptSuccess();
            _logger.Information("Rename {Status}", success ? "succeeded" : "failed");
            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception renaming file");
            return false;
        }
    }

    public bool DeleteFile(IntPtr fileEntry)
    {
        if (!_isFileOpen)
        {
            _logger.Warning("Attempted to delete file without an open PCK");
            return false;
        }

        try
        {
            _logger.Information("Deleting file");

            var result = PckNative.pck_DeleteEntry(fileEntry);
            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Failed to delete file: {Result}", result);
                return false;
            }

            result = PckNative.pck_DeleteEntrySubmit();
            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Failed to submit delete: {Result}", result);
                return false;
            }

            // Wait for operation to complete
            while (PckNative.pck_isThreadWorking())
            {
                System.Threading.Thread.Sleep(100);
            }

            var success = PckNative.pck_isLastOptSuccess();
            _logger.Information("Delete {Status}", success ? "succeeded" : "failed");
            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception deleting file");
            return false;
        }
    }

    public bool RebuildPck(string outputPath, bool useRecompress = true)
    {
        if (!_isFileOpen)
        {
            _logger.Warning("Attempted to rebuild PCK without an open file");
            return false;
        }

        try
        {
            _logger.Information("Rebuilding PCK to: {OutputPath}", outputPath);

            var result = PckNative.pck_RebuildPckFile(outputPath, useRecompress);

            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Failed to rebuild PCK: {Result}", result);
                return false;
            }

            // Wait for operation to complete
            while (PckNative.pck_isThreadWorking())
            {
                var progress = PckNative.pck_getUIProgress();
                var total = PckNative.pck_getUIProgressUpper();
                ProgressChanged?.Invoke(this, new ProgressEventArgs(progress, total));
                System.Threading.Thread.Sleep(100);
            }

            var success = PckNative.pck_isLastOptSuccess();
            _logger.Information("Rebuild {Status}", success ? "succeeded" : "failed");
            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception rebuilding PCK");
            return false;
        }
    }

    public bool BatchExtractPckFile(string pckFilePath, string destDir)
    {
        try
        {
            _logger.Information("Batch extracting PCK: {PckFile} to {DestDir}", pckFilePath, destDir);

            var result = PckNative.do_ExtractAllFiles(pckFilePath, destDir);

            if (result != PckNative.PCKRTN.WINPCK_OK)
            {
                _logger.Error("Failed to start batch extraction: {Result}", result);
                return false;
            }

            // Wait for operation to complete
            while (PckNative.pck_isThreadWorking())
            {
                var progress = PckNative.pck_getUIProgress();
                var total = PckNative.pck_getUIProgressUpper();
                ProgressChanged?.Invoke(this, new ProgressEventArgs(progress, total));
                System.Threading.Thread.Sleep(100);
            }

            var success = PckNative.pck_isLastOptSuccess();
            _logger.Information("Batch extraction {Status}", success ? "succeeded" : "failed");
            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception during batch extraction");
            return false;
        }
    }

    public void Dispose()
    {
        CloseFile();
        _logger.Information("PckService disposed");
    }
}

public class ProgressEventArgs : EventArgs
{
    public uint Current { get; }
    public uint Total { get; }
    public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;

    public ProgressEventArgs(uint current, uint total)
    {
        Current = current;
        Total = total;
    }
}
