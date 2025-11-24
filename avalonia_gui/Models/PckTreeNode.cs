using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinPCK.Avalonia.Models;

public partial class PckTreeNode : ObservableObject
{
    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _icon = "📁";

    [ObservableProperty]
    private ObservableCollection<PckTreeNode> _children = new();

    [ObservableProperty]
    private ObservableCollection<PckFileEntry> _files = new();

    public IntPtr EntryPointer { get; set; }
    public bool IsFolder { get; set; } = true;
}
