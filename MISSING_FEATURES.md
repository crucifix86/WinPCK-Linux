# WinPCK Linux GUI - Missing Features from Windows Version

## Status: Search and Extraction Complete ✅
- Extract All: **WORKING**
- Extract Selected: **WORKING**
- Search/Find: **WORKING**
- TreeView Navigation: **WORKING**
- File Info Dialog: **WORKING**

---

## Missing Features to Implement

### File Menu
- [ ] **New/Create PCK** - Create a new empty PCK archive
  - Native function: `pck_create` or similar
  - UI: File → New menu item

### Operation Menu - Write Operations
- [ ] **Add Files to PCK** - Add new files to existing PCK archive
  - Native function: `pck_AddFiles` or similar
  - UI: Operation → Add Files button/menu
  - Need file picker to select files to add

- [ ] **Rebuild PCK** - Reorganize and rebuild PCK structure
  - Native function: `pck_RebuildPck` or similar
  - UI: Operation → Rebuild menu item
  - Optimizes file layout and removes fragmentation

- [ ] **Game Streamlined** - Optimize PCK for game usage
  - Native function: TBD
  - UI: Operation → Streamline menu item

- [ ] **Compression Options** - Configure compression settings
  - Native function: `pck_setCompressLevel` or similar
  - UI: Settings dialog for compression level, threading, etc.

### File Operations (Context Menu)
- [ ] **View/Preview File** - Preview file contents
  - For text files: show in text viewer
  - For images: show image preview
  - Extract to temp location and open

- [ ] **Rename File** - Rename files within PCK
  - Native function: `pck_RenameFile` or similar
  - UI: Right-click → Rename

- [ ] **Delete File** - Remove files from PCK
  - Native function: `pck_DeleteFile` or similar
  - UI: Right-click → Delete

### Selection Operations
- [ ] **Select All** - Select all files in current view
  - UI: Ctrl+A or Edit → Select All
  - Enable multi-select in ListBox

- [ ] **Reverse Selection** - Invert current selection
  - UI: Edit → Reverse Selection

- [ ] **Extract Multiple Selected** - Extract multiple selected files
  - Extend current Extract Selected to handle multiple files
  - Need multi-select support in UI

### File Properties
- [ ] **File Properties Dialog** - Show detailed file attributes
  - File size (original/compressed)
  - Compression ratio
  - File offset in PCK
  - CRC/hash if available
  - UI: Right-click → Properties

### Other Features
- [ ] **Settings Dialog** - Application settings
  - Default extraction path
  - Compression defaults
  - UI preferences

- [ ] **About Dialog** - Version and credits
  - Application version
  - Native library version
  - Credits and license info

---

## Priority Implementation Order

### Phase 1: Context Menu & Selection (Essential for usability)
1. Enable multi-select in ListBox
2. Select All / Reverse Selection
3. Extract multiple selected files
4. Right-click context menu framework

### Phase 2: File Preview (Very useful for browsing)
5. View/Preview file functionality
6. File Properties dialog

### Phase 3: Write Operations (Modifying PCK files)
7. Add Files to PCK
8. Delete files from PCK
9. Rename files in PCK
10. Create New PCK
11. Rebuild PCK

### Phase 4: Settings & Polish
12. Compression Options dialog
13. Settings dialog
14. About dialog
15. Game Streamlined feature

---

## Technical Notes

### UTF-32 String Handling
All string parameters to native functions must use UTF-32 conversion:
```csharp
IntPtr ptr = PckNative.StringToUtf32Ptr(str);
try { /* call native */ }
finally { PckNative.FreeUtf32Ptr(ptr); }
```

### Multi-select Support
Need to change ListBox to allow multiple selection:
```xml
<ListBox SelectionMode="Multiple" ...>
```

### Context Menu
Add to ListBox:
```xml
<ListBox.ContextMenu>
    <ContextMenu>
        <MenuItem Header="View" Command="{Binding ViewFileCommand}"/>
        <MenuItem Header="Extract" Command="{Binding ExtractSelectedCommand}"/>
        <Separator/>
        <MenuItem Header="Rename" Command="{Binding RenameFileCommand}"/>
        <MenuItem Header="Delete" Command="{Binding DeleteFileCommand}"/>
        <Separator/>
        <MenuItem Header="Properties" Command="{Binding ShowPropertiesCommand}"/>
    </ContextMenu>
</ListBox.ContextMenu>
```

### Native Functions to Investigate
Check `/home/doug/WinPCK/PckDll/include/pck_handle.h` for:
- File addition functions
- File modification functions
- PCK creation functions
- Rebuild functions
