using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WinPCK.Avalonia.Models;
using WinPCK.Avalonia.Services;

namespace WinPCK.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PckService _pckService;
    private readonly ILogger _logger;
    private Window? _window;

    [ObservableProperty]
    private string _title = "WinPCK - Linux Edition";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private bool _isFileOpen;

    [ObservableProperty]
    private bool _isOperationInProgress;

    [ObservableProperty]
    private PckFileInfo? _currentFileInfo;

    [ObservableProperty]
    private ObservableCollection<PckFileEntry> _fileEntries = new();

    [ObservableProperty]
    private ObservableCollection<string> _logMessages = new();

    [ObservableProperty]
    private PckFileEntry? _selectedEntry;

    private System.Collections.IList? _selectedEntries;
    public System.Collections.IList? SelectedEntries
    {
        get => _selectedEntries;
        set
        {
            if (SetProperty(ref _selectedEntries, value))
            {
                _logger.Debug($"SelectedEntries changed, count: {value?.Count ?? 0}");
            }
        }
    }

    [ObservableProperty]
    private ObservableCollection<PckTreeNode> _treeRootNodes = new();

    [ObservableProperty]
    private PckTreeNode? _selectedTreeNode;

    [ObservableProperty]
    private ObservableCollection<PckFileEntry> _currentFolderEntries = new();

    public MainWindowViewModel()
    {
        _logger = Log.ForContext<MainWindowViewModel>();
        _pckService = new PckService(_logger);

        _pckService.LogMessageReceived += OnLogMessageReceived;
        _pckService.ProgressChanged += OnProgressChanged;

        _logger.Information("MainWindowViewModel initialized");
    }

    public void SetWindow(Window window)
    {
        _window = window;
    }

    private void OnLogMessageReceived(object? sender, string message)
    {
        LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        if (LogMessages.Count > 1000)
        {
            LogMessages.RemoveAt(LogMessages.Count - 1);
        }
    }

    partial void OnSelectedTreeNodeChanged(PckTreeNode? value)
    {
        if (value != null)
        {
            _logger.Information($"Tree node selected: {value.DisplayName}");
            LoadFolderContents(value);
        }
    }

    private void OnProgressChanged(object? sender, ProgressEventArgs e)
    {
        ProgressValue = e.Percentage;
        StatusText = $"Progress: {e.Current}/{e.Total} ({e.Percentage:F1}%)";
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        if (_window == null)
        {
            AddLogMessage("ERROR", "Window not initialized");
            return;
        }

        try
        {
            _logger.Information("OpenFile command executed");

            // Open file dialog
            var storageProvider = _window.StorageProvider;
            var fileTypes = new FilePickerFileType[]
            {
                new("PCK Files") { Patterns = new[] { "*.pck", "*.cup", "*.zup" } },
                new("All Files") { Patterns = new[] { "*.*" } }
            };

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open PCK File",
                AllowMultiple = false,
                FileTypeFilter = fileTypes
            });

            if (files == null || files.Count == 0)
            {
                AddLogMessage("INFO", "File selection cancelled");
                return;
            }

            var selectedFile = files[0];
            var filePath = selectedFile.Path.LocalPath;

            AddLogMessage("INFO", $"Opening file: {filePath}");

            if (_pckService.OpenFile(filePath))
            {
                IsFileOpen = true;
                CurrentFileInfo = _pckService.GetFileInfo();

                if (CurrentFileInfo != null)
                {
                    Title = $"WinPCK - {Path.GetFileName(filePath)}";
                    AddLogMessage("SUCCESS", $"Opened: {CurrentFileInfo.Version}, {CurrentFileInfo.FileCount} files");
                }

                await LoadFileList();
                await BuildTreeView();
            }
            else
            {
                AddLogMessage("ERROR", "Failed to open PCK file");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in OpenFile command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CloseFile()
    {
        try
        {
            _logger.Information("CloseFile command executed");
            _pckService.CloseFile();
            IsFileOpen = false;
            CurrentFileInfo = null;
            FileEntries.Clear();
            TreeRootNodes.Clear();
            CurrentFolderEntries.Clear();
            Title = "WinPCK - Linux Edition";
            AddLogMessage("INFO", "File closed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in CloseFile command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExtractAll()
    {
        if (!IsFileOpen)
        {
            AddLogMessage("WARNING", "No file is open");
            return;
        }

        if (_window == null)
        {
            AddLogMessage("ERROR", "Window not initialized");
            return;
        }

        try
        {
            _logger.Information("ExtractAll command executed");

            // Open folder picker
            var storageProvider = _window.StorageProvider;
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Extraction Destination",
                AllowMultiple = false
            });

            if (folders == null || folders.Count == 0)
            {
                AddLogMessage("INFO", "Folder selection cancelled");
                return;
            }

            var destDir = folders[0].Path.LocalPath;

            IsOperationInProgress = true;
            AddLogMessage("INFO", $"Extracting to: {destDir}");

            await Task.Run(() =>
            {
                var success = _pckService.ExtractAllFiles(destDir);
                if (success)
                {
                    AddLogMessage("SUCCESS", "All files extracted successfully");
                }
                else
                {
                    AddLogMessage("ERROR", "Extraction failed");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in ExtractAll command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    [RelayCommand]
    private async Task LoadFileList()
    {
        if (!IsFileOpen)
            return;

        try
        {
            _logger.Information("Loading file list");
            FileEntries.Clear();

            var entries = await Task.Run(() => _pckService.ListFiles());

            foreach (var entry in entries)
            {
                FileEntries.Add(entry);
            }

            AddLogMessage("INFO", $"Loaded {FileEntries.Count} entries");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading file list");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowInfo()
    {
        if (CurrentFileInfo == null)
        {
            AddLogMessage("WARNING", "No file info available");
            return;
        }

        AddLogMessage("INFO", "=== PCK File Information ===");
        AddLogMessage("INFO", $"  Version: {CurrentFileInfo.Version} (ID: {CurrentFileInfo.VersionId})");
        AddLogMessage("INFO", $"  File Count: {CurrentFileInfo.FileCount}");
        AddLogMessage("INFO", $"  Total Size: {CurrentFileInfo.TotalSizeMB}");
        AddLogMessage("INFO", $"  Data Area: {CurrentFileInfo.DataAreaSizeMB}");
        AddLogMessage("INFO", $"  Redundancy: {CurrentFileInfo.RedundancySizeMB}");
        AddLogMessage("INFO", $"  Supports Update: {CurrentFileInfo.SupportsUpdate}");
        if (!string.IsNullOrWhiteSpace(CurrentFileInfo.AdditionalInfo))
        {
            AddLogMessage("INFO", $"  Additional Info: {CurrentFileInfo.AdditionalInfo}");
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogMessages.Clear();
        AddLogMessage("INFO", "Log cleared");
    }

    [RelayCommand]
    private async Task ExtractSelected()
    {
        _logger.Information($"ExtractSelected - SelectedEntries is null: {SelectedEntries == null}, Count: {SelectedEntries?.Count ?? 0}");

        if (!IsFileOpen || SelectedEntries == null || SelectedEntries.Count == 0)
        {
            AddLogMessage("WARNING", $"No files selected (SelectedEntries null: {SelectedEntries == null})");
            return;
        }

        if (_window == null)
        {
            AddLogMessage("ERROR", "Window not initialized");
            return;
        }

        try
        {
            _logger.Information($"ExtractSelected command executed for {SelectedEntries.Count} files");

            // Open folder picker
            var storageProvider = _window.StorageProvider;
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Extraction Destination",
                AllowMultiple = false
            });

            if (folders == null || folders.Count == 0)
            {
                AddLogMessage("INFO", "Folder selection cancelled");
                return;
            }

            var destDir = folders[0].Path.LocalPath;

            IsOperationInProgress = true;
            AddLogMessage("INFO", $"Extracting {SelectedEntries.Count} selected files to: {destDir}");

            await Task.Run(() =>
            {
                var fileEntries = SelectedEntries.Cast<PckFileEntry>()
                                                .Select(e => e.EntryPointer)
                                                .ToArray();

                _logger.Information($"Extracting {fileEntries.Length} entries");
                var success = _pckService.ExtractSelectedFiles(fileEntries, fileEntries.Length, destDir);

                if (success)
                {
                    AddLogMessage("SUCCESS", $"Extracted {fileEntries.Length} files successfully");
                }
                else
                {
                    AddLogMessage("ERROR", "Extraction failed");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in ExtractSelected command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        if (!IsFileOpen || CurrentFolderEntries.Count == 0)
        {
            AddLogMessage("WARNING", "No files to select");
            return;
        }

        try
        {
            _logger.Information($"SelectAll command - SelectedEntries is null: {SelectedEntries == null}, Count: {CurrentFolderEntries.Count}");

            if (SelectedEntries != null)
            {
                _logger.Information($"SelectedEntries before clear: {SelectedEntries.Count}");
                SelectedEntries.Clear();
                foreach (var entry in CurrentFolderEntries)
                {
                    SelectedEntries.Add(entry);
                }
                _logger.Information($"SelectedEntries after adding: {SelectedEntries.Count}");
                AddLogMessage("INFO", $"Selected all {SelectedEntries.Count} items");
            }
            else
            {
                AddLogMessage("ERROR", "SelectedEntries collection is null - binding may not be working");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in SelectAll command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ReverseSelection()
    {
        if (!IsFileOpen || CurrentFolderEntries.Count == 0)
        {
            AddLogMessage("WARNING", "No files to select");
            return;
        }

        try
        {
            if (SelectedEntries != null)
            {
                var currentlySelected = SelectedEntries.Cast<PckFileEntry>().ToList();
                SelectedEntries.Clear();

                foreach (var entry in CurrentFolderEntries)
                {
                    if (!currentlySelected.Contains(entry))
                    {
                        SelectedEntries.Add(entry);
                    }
                }

                AddLogMessage("INFO", $"Reversed selection - now {SelectedEntries.Count} items selected");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in ReverseSelection command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        if (!IsFileOpen)
        {
            AddLogMessage("WARNING", "No file is open");
            return;
        }

        if (_window == null)
        {
            AddLogMessage("ERROR", "Window not initialized");
            return;
        }

        try
        {
            // Simple text input dialog
            var searchText = await ShowInputDialog("Search Files", "Enter search text:");

            if (string.IsNullOrWhiteSpace(searchText))
            {
                AddLogMessage("INFO", "Search cancelled");
                return;
            }

            AddLogMessage("INFO", $"Searching for: {searchText}");

            var results = await Task.Run(() => _pckService.SearchFiles(searchText));

            CurrentFolderEntries.Clear();
            foreach (var result in results)
            {
                CurrentFolderEntries.Add(result);
            }

            AddLogMessage("SUCCESS", $"Found {results.Count} matching files");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in Search command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
    }

    private async Task<string> ShowInputDialog(string title, string prompt)
    {
        var dialog = new Views.SearchDialog();
        var result = await dialog.ShowDialog<bool>(_window!);

        if (result && !string.IsNullOrWhiteSpace(dialog.SearchText))
        {
            return dialog.SearchText;
        }

        return "";
    }

    private void AddLogMessage(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogMessages.Insert(0, $"[{timestamp}] [{level}] {message}");
        if (LogMessages.Count > 1000)
        {
            LogMessages.RemoveAt(LogMessages.Count - 1);
        }
    }

    [RelayCommand]
    private async Task BuildTreeView()
    {
        if (!IsFileOpen)
            return;

        try
        {
            _logger.Information("Building tree view");
            TreeRootNodes.Clear();

            var rootNode = await Task.Run(() =>
            {
                var root = new PckTreeNode
                {
                    DisplayName = "Root",
                    Icon = "📁",
                    EntryPointer = IntPtr.Zero,
                    IsFolder = true
                };

                var visitedNodes = new HashSet<IntPtr>();
                BuildTreeNodeRecursive(root, IntPtr.Zero, visitedNodes);

                return root;
            });

            TreeRootNodes.Add(rootNode);

            // Select root by default
            if (TreeRootNodes.Count > 0)
            {
                SelectedTreeNode = TreeRootNodes[0];
            }

            AddLogMessage("INFO", "Tree view built successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error building tree view");
            AddLogMessage("ERROR", $"Failed to build tree: {ex.Message}");
        }
    }

    private void BuildTreeNodeRecursive(PckTreeNode parentNode, IntPtr entryPtr, HashSet<IntPtr> visitedNodes)
    {
        // Prevent infinite recursion on circular references
        if (entryPtr != IntPtr.Zero && visitedNodes.Contains(entryPtr))
            return;

        if (entryPtr != IntPtr.Zero)
            visitedNodes.Add(entryPtr);

        var entries = _pckService.ListFiles(entryPtr == IntPtr.Zero ? null : entryPtr);

        foreach (var entry in entries)
        {
            if (entry.IsFolder)
            {
                var childNode = new PckTreeNode
                {
                    DisplayName = entry.Name,
                    Icon = "📁",
                    EntryPointer = entry.EntryPointer,
                    IsFolder = true
                };

                // Recursively build child folders
                BuildTreeNodeRecursive(childNode, entry.EntryPointer, visitedNodes);

                parentNode.Children.Add(childNode);
            }
            else
            {
                parentNode.Files.Add(entry);
            }
        }
    }

    private void LoadFolderContents(PckTreeNode node)
    {
        try
        {
            _logger.Information($"Loading contents for: {node.DisplayName} (Files: {node.Files.Count}, Children: {node.Children.Count})");

            // Ensure UI updates happen on the UI thread
            Dispatcher.UIThread.Post(() =>
            {
                CurrentFolderEntries.Clear();

                // Add child folders first
                foreach (var child in node.Children)
                {
                    CurrentFolderEntries.Add(new PckFileEntry
                    {
                        Name = child.DisplayName,
                        EntryType = 0x01, // Folder type
                        Size = 0,
                        CompressedSize = 0,
                        EntryPointer = child.EntryPointer
                    });
                }

                // Add all files in this folder
                foreach (var file in node.Files)
                {
                    CurrentFolderEntries.Add(file);
                }

                _logger.Information($"Loaded {CurrentFolderEntries.Count} entries to CurrentFolderEntries");
                AddLogMessage("INFO", $"Loaded folder: {node.DisplayName} ({CurrentFolderEntries.Count} items)");
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading folder contents");
            AddLogMessage("ERROR", $"Failed to load folder: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CreateNewPck()
    {
        if (_window == null)
        {
            AddLogMessage("ERROR", "Window not initialized");
            return;
        }

        try
        {
            _logger.Information("CreateNewPck command executed");

            // Select source folder
            var storageProvider = _window.StorageProvider;
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Source Folder",
                AllowMultiple = false
            });

            if (folders == null || folders.Count == 0)
            {
                AddLogMessage("INFO", "Folder selection cancelled");
                return;
            }

            var srcPath = folders[0].Path.LocalPath;

            // Select destination PCK file
            var pckFile = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save New PCK File",
                SuggestedFileName = "newfile.pck",
                FileTypeChoices = new[] { new FilePickerFileType("PCK Files") { Patterns = new[] { "*.pck" } } }
            });

            if (pckFile == null)
            {
                AddLogMessage("INFO", "Save cancelled");
                return;
            }

            var dstPath = pckFile.Path.LocalPath;

            IsOperationInProgress = true;
            AddLogMessage("INFO", $"Creating PCK from: {srcPath}");

            await Task.Run(() =>
            {
                var success = _pckService.CreateNewPck(srcPath, dstPath, 9);
                if (success)
                {
                    AddLogMessage("SUCCESS", $"PCK created successfully: {dstPath}");
                }
                else
                {
                    AddLogMessage("ERROR", "PCK creation failed");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in CreateNewPck command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    [RelayCommand]
    private async Task AddFiles()
    {
        if (!IsFileOpen || _window == null)
        {
            AddLogMessage("WARNING", "No PCK file is open");
            return;
        }

        try
        {
            _logger.Information("AddFiles command executed");

            // Select files or folder to add
            var storageProvider = _window.StorageProvider;
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder to Add",
                AllowMultiple = false
            });

            if (folders == null || folders.Count == 0)
            {
                AddLogMessage("INFO", "Selection cancelled");
                return;
            }

            var srcPath = folders[0].Path.LocalPath;

            IsOperationInProgress = true;
            AddLogMessage("INFO", $"Adding files from: {srcPath}");

            await Task.Run(() =>
            {
                var currentPath = _pckService.GetFileInfo()?.FilePath ?? "";
                var success = _pckService.AddFilesToPck(srcPath, currentPath, "", 9);
                if (success)
                {
                    AddLogMessage("SUCCESS", "Files added successfully");
                    // Reload the file list
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await LoadFileList();
                        await BuildTreeView();
                    });
                }
                else
                {
                    AddLogMessage("ERROR", "Adding files failed");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in AddFiles command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    [RelayCommand]
    private async Task RenameFile()
    {
        if (!IsFileOpen || SelectedEntry == null || _window == null)
        {
            AddLogMessage("WARNING", "No file selected");
            return;
        }

        try
        {
            _logger.Information("RenameFile command executed");

            // Show input dialog for new name
            var dialog = new Views.SearchDialog(); // We'll reuse this for now
            var result = await dialog.ShowDialog<bool>(_window);

            if (!result || string.IsNullOrWhiteSpace(dialog.SearchText))
            {
                AddLogMessage("INFO", "Rename cancelled");
                return;
            }

            var newName = dialog.SearchText;

            IsOperationInProgress = true;
            AddLogMessage("INFO", $"Renaming to: {newName}");

            await Task.Run(() =>
            {
                var success = _pckService.RenameFile(SelectedEntry.EntryPointer, newName);
                if (success)
                {
                    AddLogMessage("SUCCESS", $"File renamed to: {newName}");
                    // Reload the file list
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await LoadFileList();
                        await BuildTreeView();
                    });
                }
                else
                {
                    AddLogMessage("ERROR", "Rename failed");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in RenameFile command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    [RelayCommand]
    private async Task DeleteFile()
    {
        if (!IsFileOpen || SelectedEntry == null)
        {
            AddLogMessage("WARNING", "No file selected");
            return;
        }

        try
        {
            _logger.Information("DeleteFile command executed");

            IsOperationInProgress = true;
            AddLogMessage("INFO", $"Deleting: {SelectedEntry.Name}");

            await Task.Run(() =>
            {
                var success = _pckService.DeleteFile(SelectedEntry.EntryPointer);
                if (success)
                {
                    AddLogMessage("SUCCESS", $"File deleted: {SelectedEntry.Name}");
                    // Reload the file list
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await LoadFileList();
                        await BuildTreeView();
                    });
                }
                else
                {
                    AddLogMessage("ERROR", "Delete failed");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in DeleteFile command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    [RelayCommand]
    private async Task BatchExtract()
    {
        if (_window == null)
        {
            AddLogMessage("ERROR", "Window not initialized");
            return;
        }

        try
        {
            _logger.Information("BatchExtract command executed");

            // Select multiple PCK files
            var storageProvider = _window.StorageProvider;
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select PCK Files to Extract",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("PCK Files") { Patterns = new[] { "*.pck", "*.cup", "*.zup" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files == null || files.Count == 0)
            {
                AddLogMessage("INFO", "File selection cancelled");
                return;
            }

            // Select destination folder
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Extraction Destination",
                AllowMultiple = false
            });

            if (folders == null || folders.Count == 0)
            {
                AddLogMessage("INFO", "Folder selection cancelled");
                return;
            }

            var destDir = folders[0].Path.LocalPath;

            IsOperationInProgress = true;
            AddLogMessage("INFO", $"Batch extracting {files.Count} PCK files to: {destDir}");

            await Task.Run(() =>
            {
                int successCount = 0;
                int failCount = 0;

                for (int i = 0; i < files.Count; i++)
                {
                    var pckFile = files[i].Path.LocalPath;
                    var fileName = System.IO.Path.GetFileName(pckFile);

                    AddLogMessage("INFO", $"[{i + 1}/{files.Count}] Extracting: {fileName}");

                    var success = _pckService.BatchExtractPckFile(pckFile, destDir);

                    if (success)
                    {
                        successCount++;
                        AddLogMessage("SUCCESS", $"[{i + 1}/{files.Count}] Extracted: {fileName}");
                    }
                    else
                    {
                        failCount++;
                        AddLogMessage("ERROR", $"[{i + 1}/{files.Count}] Failed: {fileName}");
                    }
                }

                AddLogMessage("INFO", $"Batch extraction complete: {successCount} succeeded, {failCount} failed");
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in BatchExtract command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    [RelayCommand]
    private async Task RebuildPck()
    {
        if (!IsFileOpen || _window == null)
        {
            AddLogMessage("WARNING", "No PCK file is open");
            return;
        }

        try
        {
            _logger.Information("RebuildPck command executed");

            // Select destination for rebuilt PCK
            var storageProvider = _window.StorageProvider;
            var pckFile = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Rebuilt PCK File",
                SuggestedFileName = "rebuilt.pck",
                FileTypeChoices = new[] { new FilePickerFileType("PCK Files") { Patterns = new[] { "*.pck" } } }
            });

            if (pckFile == null)
            {
                AddLogMessage("INFO", "Rebuild cancelled");
                return;
            }

            var outputPath = pckFile.Path.LocalPath;

            IsOperationInProgress = true;
            AddLogMessage("INFO", $"Rebuilding PCK to: {outputPath}");

            await Task.Run(() =>
            {
                var success = _pckService.RebuildPck(outputPath, true);
                if (success)
                {
                    AddLogMessage("SUCCESS", $"PCK rebuilt successfully: {outputPath}");
                }
                else
                {
                    AddLogMessage("ERROR", "Rebuild failed");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in RebuildPck command");
            AddLogMessage("ERROR", $"Exception: {ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }
}
