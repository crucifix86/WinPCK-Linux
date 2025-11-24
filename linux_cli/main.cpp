/*
 * Linux CLI tool for WinPCK
 * Provides command-line interface for PCK file operations
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <locale.h>
#include <wchar.h>
#include <string>
#include <iostream>

// Include PCK library header
#include "pck_handle.h"

void print_usage(const char* program) {
    printf("WinPCK Linux CLI v1.33.1\n");
    printf("Perfect World PCK file tool\n\n");
    printf("Usage: %s <command> [options]\n\n", program);
    printf("Commands:\n");
    printf("  list <pck_file> [path]         - List contents of PCK file\n");
    printf("  extract <pck_file> <dest_dir>  - Extract all files from PCK\n");
    printf("  info <pck_file>                - Show PCK file information\n");
    printf("  create <src_dir> <pck_file>    - Create new PCK file\n");
    printf("  add <pck_file> <file> [path]   - Add file to PCK\n");
    printf("\nExamples:\n");
    printf("  %s list game.pck\n", program);
    printf("  %s extract game.pck ./output\n", program);
    printf("  %s info game.pck\n", program);
    printf("\n");
}

// Convert char* to wchar_t*
std::wstring char_to_wstring(const char* str) {
    if (!str) return L"";

    size_t len = strlen(str);
    if (len == 0) return L"";

    size_t wlen = mbstowcs(NULL, str, 0);
    if (wlen == (size_t)-1) {
        std::cerr << "Error converting string to wide char" << std::endl;
        return L"";
    }

    std::wstring result(wlen, L'\0');
    mbstowcs(&result[0], str, len + 1);
    return result;
}

// Callback for logging
void log_callback(char level, const wchar_t* msg) {
    wprintf(L"[%c] %ls\n", level, msg);
}

// Callback for listing files
void list_callback(void* param, int32_t sn, const wchar_t* szName, int32_t entryType,
                  uint64_t dwFileClearTextSize, uint64_t dwFileCipherTextSize, void* fileEntry) {
    if (!szName) return;

    // Print entry based on type
    if (entryType & PCK_ENTRY_TYPE_INDEX) {
        // File
        if (dwFileClearTextSize > 0) {
            double ratio = dwFileCipherTextSize > 0 ? (100.0 * dwFileCipherTextSize / dwFileClearTextSize) : 0;
            printf("[FILE] %ls (%llu bytes, compressed: %llu bytes, %.1f%%)\n",
                   szName, dwFileClearTextSize, dwFileCipherTextSize, ratio);
        } else {
            printf("[FILE] %ls\n", szName);
        }
    } else if (entryType & PCK_ENTRY_TYPE_FOLDER) {
        // Directory
        printf("[DIR]  %ls\n", szName);
    }
}

int cmd_list(const char* pck_file, const char* path) {
    std::wstring wpck = char_to_wstring(pck_file);

    printf("Opening PCK file: %s\n", pck_file);

    PCKRTN ret = pck_open(wpck.c_str());
    if (ret != WINPCK_OK) {
        fprintf(stderr, "Error: Failed to open PCK file\n");
        return 1;
    }

    if (!pck_IsValidPck()) {
        fprintf(stderr, "Error: Invalid PCK file\n");
        pck_close();
        return 1;
    }

    printf("PCK file opened successfully\n");
    printf("Version: %ls\n", pck_GetCurrentVersionName());
    printf("Files: %u\n", pck_filecount());
    printf("File size: %llu bytes\n", pck_filesize());
    printf("\nContents:\n");
    printf("--------\n");
    fflush(stdout);

    if (path) {
        std::wstring wpath = char_to_wstring(path);
        LPCENTRY entry = pck_getFileEntryByPath((LPWSTR)wpath.c_str());
        if (entry) {
            pck_listByNode(entry, nullptr, list_callback);
        } else {
            fprintf(stderr, "Error: Path not found in PCK\n");
        }
    } else {
        LPCENTRY root = pck_getRootNode();
        uint32_t count = pck_listByNode(root, nullptr, list_callback);
        printf("\n\nTotal entries: %u\n", count);
    }

    pck_close();
    return 0;
}

int cmd_extract(const char* pck_file, const char* dest_dir) {
    std::wstring wpck = char_to_wstring(pck_file);
    std::wstring wdest = char_to_wstring(dest_dir);

    printf("Opening PCK file: %s\n", pck_file);

    PCKRTN ret = pck_open(wpck.c_str());
    if (ret != WINPCK_OK) {
        fprintf(stderr, "Error: Failed to open PCK file\n");
        return 1;
    }

    if (!pck_IsValidPck()) {
        fprintf(stderr, "Error: Invalid PCK file\n");
        pck_close();
        return 1;
    }

    printf("Extracting all files to: %s\n", dest_dir);

    ret = pck_ExtractAllFiles(wdest.c_str());
    if (ret != WINPCK_OK) {
        fprintf(stderr, "Error: Failed to extract files\n");
        pck_close();
        return 1;
    }

    // Wait for extraction to complete
    while (pck_isThreadWorking()) {
        usleep(100000); // 100ms
        uint32_t progress = pck_getUIProgress();
        uint32_t total = pck_getUIProgressUpper();
        if (total > 0) {
            printf("\rProgress: %u / %u (%.1f%%)", progress, total,
                   100.0 * progress / total);
            fflush(stdout);
        }
    }
    printf("\n");

    if (!pck_isLastOptSuccess()) {
        fprintf(stderr, "Error: Extraction failed\n");
        pck_close();
        return 1;
    }

    printf("Extraction completed successfully\n");
    pck_close();
    return 0;
}

int cmd_info(const char* pck_file) {
    std::wstring wpck = char_to_wstring(pck_file);

    printf("Opening PCK file: %s\n", pck_file);

    PCKRTN ret = pck_open(wpck.c_str());
    if (ret != WINPCK_OK) {
        fprintf(stderr, "Error: Failed to open PCK file\n");
        return 1;
    }

    if (!pck_IsValidPck()) {
        fprintf(stderr, "Error: Invalid PCK file\n");
        pck_close();
        return 1;
    }

    printf("\nPCK File Information:\n");
    printf("=====================\n");
    printf("Version: %ls\n", pck_GetCurrentVersionName());
    printf("Version ID: %d\n", pck_getVersion());
    printf("File count: %u\n", pck_filecount());
    printf("File size: %llu bytes (%.2f MB)\n", pck_filesize(), pck_filesize() / 1048576.0);
    printf("Data area size: %llu bytes (%.2f MB)\n",
           pck_file_data_area_size(), pck_file_data_area_size() / 1048576.0);
    printf("Redundant data: %llu bytes (%.2f MB)\n",
           pck_file_redundancy_data_size(), pck_file_redundancy_data_size() / 1048576.0);
    printf("Supports updates: %s\n", pck_isSupportAddFileToPck() ? "Yes" : "No");

    const char* info = pck_GetAdditionalInfo();
    if (info && strlen(info) > 0) {
        printf("Additional info: %s\n", info);
    }

    pck_close();
    return 0;
}

int cmd_create(const char* src_dir, const char* pck_file) {
    std::wstring wsrc = char_to_wstring(src_dir);
    std::wstring wpck = char_to_wstring(pck_file);

    printf("Creating PCK file: %s from %s\n", pck_file, src_dir);

    PCKRTN ret = do_CreatePckFile(wsrc.c_str(), wpck.c_str(), 0, 9);
    if (ret != WINPCK_OK) {
        fprintf(stderr, "Error: Failed to create PCK file\n");
        return 1;
    }

    // Wait for operation to complete
    while (pck_isThreadWorking()) {
        usleep(100000); // 100ms
        uint32_t progress = pck_getUIProgress();
        uint32_t total = pck_getUIProgressUpper();
        if (total > 0) {
            printf("\rProgress: %u / %u (%.1f%%)", progress, total,
                   100.0 * progress / total);
            fflush(stdout);
        }
    }
    printf("\n");

    if (!pck_isLastOptSuccess()) {
        fprintf(stderr, "Error: Creation failed\n");
        return 1;
    }

    printf("PCK file created successfully\n");
    return 0;
}

int main(int argc, char* argv[]) {
    // Set locale for wide character support
    setlocale(LC_ALL, "");

    if (argc < 2) {
        print_usage(argv[0]);
        return 1;
    }

    // Register log callback
    log_regShowFunc(log_callback);

    const char* command = argv[1];

    if (strcmp(command, "list") == 0) {
        if (argc < 3) {
            fprintf(stderr, "Error: Missing PCK file argument\n");
            print_usage(argv[0]);
            return 1;
        }
        const char* path = (argc >= 4) ? argv[3] : nullptr;
        return cmd_list(argv[2], path);
    }
    else if (strcmp(command, "extract") == 0) {
        if (argc < 4) {
            fprintf(stderr, "Error: Missing arguments\n");
            print_usage(argv[0]);
            return 1;
        }
        return cmd_extract(argv[2], argv[3]);
    }
    else if (strcmp(command, "info") == 0) {
        if (argc < 3) {
            fprintf(stderr, "Error: Missing PCK file argument\n");
            print_usage(argv[0]);
            return 1;
        }
        return cmd_info(argv[2]);
    }
    else if (strcmp(command, "create") == 0) {
        if (argc < 4) {
            fprintf(stderr, "Error: Missing arguments\n");
            print_usage(argv[0]);
            return 1;
        }
        return cmd_create(argv[2], argv[3]);
    }
    else {
        fprintf(stderr, "Error: Unknown command: %s\n", command);
        print_usage(argv[0]);
        return 1;
    }

    return 0;
}
