#pragma once
/*
 * File API stubs for Linux
 * These are simplified implementations of Windows file APIs
 */

#ifndef _WIN32

#include <sys/stat.h>
#include <sys/statvfs.h>
#include <dirent.h>
#include <wchar.h>
#include <string.h>
#include <stdlib.h>
#include <stdint.h>
#include <limits.h>
#include <unistd.h>

// ULARGE_INTEGER structure
#ifndef _ULARGE_INTEGER_DEFINED
#define _ULARGE_INTEGER_DEFINED
typedef union _ULARGE_INTEGER {
    struct {
        uint32_t LowPart;
        uint32_t HighPart;
    };
    uint64_t QuadPart;
} ULARGE_INTEGER;
#endif

// WIN32_FIND_DATA structure (simplified)
typedef struct _WIN32_FIND_DATAW {
    uint32_t dwFileAttributes;
    uint64_t nFileSizeHigh;
    uint64_t nFileSizeLow;
    wchar_t cFileName[260];
    wchar_t cAlternateFileName[14];  // 8.3 filename
} WIN32_FIND_DATAW;

// HANDLE type - use consistent void* type across all files
#ifndef HANDLE
typedef void* HANDLE;
#endif
#ifndef INVALID_HANDLE_VALUE
#define INVALID_HANDLE_VALUE ((HANDLE)(long long)-1)
#endif

// Get disk free space
inline int GetDiskFreeSpaceExW(const wchar_t* lpDirectoryName, ULARGE_INTEGER* lpFreeBytesAvailable,
                               ULARGE_INTEGER* lpTotalNumberOfBytes, ULARGE_INTEGER* lpTotalNumberOfFreeBytes) {
    if (!lpDirectoryName) return 0;

    char mb_path[PATH_MAX];
    size_t len = wcstombs(mb_path, lpDirectoryName, sizeof(mb_path) - 1);
    if (len == (size_t)-1) return 0;
    mb_path[len] = '\0';

    struct statvfs stat;
    if (statvfs(mb_path, &stat) != 0) return 0;

    uint64_t free_bytes = (uint64_t)stat.f_bavail * stat.f_bsize;
    uint64_t total_bytes = (uint64_t)stat.f_blocks * stat.f_bsize;

    if (lpFreeBytesAvailable) {
        lpFreeBytesAvailable->QuadPart = free_bytes;
    }
    if (lpTotalNumberOfBytes) {
        lpTotalNumberOfBytes->QuadPart = total_bytes;
    }
    if (lpTotalNumberOfFreeBytes) {
        lpTotalNumberOfFreeBytes->QuadPart = free_bytes;
    }

    return 1;
}

inline int GetDiskFreeSpaceExA(const char* lpDirectoryName, ULARGE_INTEGER* lpFreeBytesAvailable,
                               ULARGE_INTEGER* lpTotalNumberOfBytes, ULARGE_INTEGER* lpTotalNumberOfFreeBytes) {
    if (!lpDirectoryName) return 0;

    struct statvfs stat;
    if (statvfs(lpDirectoryName, &stat) != 0) return 0;

    uint64_t free_bytes = (uint64_t)stat.f_bavail * stat.f_bsize;
    uint64_t total_bytes = (uint64_t)stat.f_blocks * stat.f_bsize;

    if (lpFreeBytesAvailable) {
        lpFreeBytesAvailable->QuadPart = free_bytes;
    }
    if (lpTotalNumberOfBytes) {
        lpTotalNumberOfBytes->QuadPart = total_bytes;
    }
    if (lpTotalNumberOfFreeBytes) {
        lpTotalNumberOfFreeBytes->QuadPart = free_bytes;
    }

    return 1;
}

#define GetDiskFreeSpaceEx GetDiskFreeSpaceExW

// File attribute functions
inline uint32_t GetFileAttributesW(const wchar_t* lpFileName) {
    if (!lpFileName) return 0xFFFFFFFF;

    char mb_path[PATH_MAX];
    size_t len = wcstombs(mb_path, lpFileName, sizeof(mb_path) - 1);
    if (len == (size_t)-1) return 0xFFFFFFFF;
    mb_path[len] = '\0';

    struct stat st;
    if (stat(mb_path, &st) != 0) return 0xFFFFFFFF;

    uint32_t attrs = 0;
    if (S_ISDIR(st.st_mode)) attrs |= 0x10; // FILE_ATTRIBUTE_DIRECTORY

    return attrs;
}

// Path functions
inline int PathFileExistsW(const wchar_t* pszPath) {
    if (!pszPath) return 0;

    char mb_path[PATH_MAX];
    size_t len = wcstombs(mb_path, pszPath, sizeof(mb_path) - 1);
    if (len == (size_t)-1) return 0;
    mb_path[len] = '\0';

    return (access(mb_path, F_OK) == 0) ? 1 : 0;
}

inline int PathIsDirectoryW(const wchar_t* pszPath) {
    if (!pszPath) return 0;

    char mb_path[PATH_MAX];
    size_t len = wcstombs(mb_path, pszPath, sizeof(mb_path) - 1);
    if (len == (size_t)-1) return 0;
    mb_path[len] = '\0';

    struct stat st;
    if (stat(mb_path, &st) != 0) return 0;

    return S_ISDIR(st.st_mode) ? 1 : 0;
}

// GetFullPathName
inline uint32_t GetFullPathNameW(const wchar_t* lpFileName, uint32_t nBufferLength,
                                  wchar_t* lpBuffer, wchar_t** lpFilePart) {
    if (!lpFileName || !lpBuffer || nBufferLength == 0) return 0;

    char mb_input[PATH_MAX];
    char mb_output[PATH_MAX];

    size_t len = wcstombs(mb_input, lpFileName, sizeof(mb_input) - 1);
    if (len == (size_t)-1) return 0;
    mb_input[len] = '\0';

    if (!realpath(mb_input, mb_output)) {
        // If realpath fails, just copy the input
        strncpy(mb_output, mb_input, sizeof(mb_output) - 1);
        mb_output[sizeof(mb_output) - 1] = '\0';
    }

    size_t wlen = mbstowcs(lpBuffer, mb_output, nBufferLength - 1);
    if (wlen == (size_t)-1) return 0;
    lpBuffer[wlen] = L'\0';

    if (lpFilePart) {
        wchar_t* lastSlash = wcsrchr(lpBuffer, L'/');
        *lpFilePart = lastSlash ? (lastSlash + 1) : lpBuffer;
    }

    return wlen;
}

// Directory search handle structure
struct _FindFileHandle {
    DIR* dir;
    char pattern[PATH_MAX];
    char dirpath[PATH_MAX];
};

// FindFirstFile / FindNextFile implementation using POSIX opendir/readdir
inline HANDLE FindFirstFileW(const wchar_t* lpFileName, WIN32_FIND_DATAW* lpFindFileData) {
    if (!lpFileName || !lpFindFileData) return INVALID_HANDLE_VALUE;

    char mb_path[PATH_MAX];
    size_t len = wcstombs(mb_path, lpFileName, sizeof(mb_path) - 1);
    if (len == (size_t)-1) return INVALID_HANDLE_VALUE;
    mb_path[len] = '\0';

    // Extract directory and pattern (e.g., "/path/to/*" -> "/path/to" and "*")
    char* last_slash = strrchr(mb_path, '/');
    char dirpath[PATH_MAX];
    char pattern[PATH_MAX];

    if (last_slash) {
        size_t dir_len = last_slash - mb_path;
        if (dir_len == 0) {
            strcpy(dirpath, "/");
        } else {
            strncpy(dirpath, mb_path, dir_len);
            dirpath[dir_len] = '\0';
        }
        strcpy(pattern, last_slash + 1);
    } else {
        strcpy(dirpath, ".");
        strcpy(pattern, mb_path);
    }

    DIR* dir = opendir(dirpath);
    if (!dir) return INVALID_HANDLE_VALUE;

    // Create handle structure
    _FindFileHandle* handle = new _FindFileHandle;
    handle->dir = dir;
    strcpy(handle->pattern, pattern);
    strcpy(handle->dirpath, dirpath);

    // Read first entry
    struct dirent* entry;
    while ((entry = readdir(dir)) != nullptr) {
        // Simple wildcard matching (support * only)
        bool match = false;
        if (strcmp(pattern, "*") == 0 || strcmp(pattern, "*.*") == 0) {
            match = true;
        } else if (strcmp(pattern, entry->d_name) == 0) {
            match = true;
        }

        if (match) {
            // Fill in find data
            mbstowcs(lpFindFileData->cFileName, entry->d_name, 260);
            lpFindFileData->cFileName[259] = L'\0';

            // Get file attributes
            char full_path[PATH_MAX];
            snprintf(full_path, sizeof(full_path), "%s/%s", dirpath, entry->d_name);
            struct stat st;
            lpFindFileData->dwFileAttributes = 0;
            if (stat(full_path, &st) == 0) {
                if (S_ISDIR(st.st_mode)) {
                    lpFindFileData->dwFileAttributes |= 0x10; // FILE_ATTRIBUTE_DIRECTORY
                }
                lpFindFileData->nFileSizeLow = st.st_size & 0xFFFFFFFF;
                lpFindFileData->nFileSizeHigh = (st.st_size >> 32) & 0xFFFFFFFF;
            }

            // Empty alternate filename for now
            lpFindFileData->cAlternateFileName[0] = L'\0';

            return (HANDLE)handle;
        }
    }

    closedir(dir);
    delete handle;
    return INVALID_HANDLE_VALUE;
}

inline int FindNextFileW(HANDLE hFindFile, WIN32_FIND_DATAW* lpFindFileData) {
    if (hFindFile == INVALID_HANDLE_VALUE || !lpFindFileData) return 0;

    _FindFileHandle* handle = (_FindFileHandle*)hFindFile;
    if (!handle || !handle->dir) return 0;

    struct dirent* entry;
    while ((entry = readdir(handle->dir)) != nullptr) {
        // Simple wildcard matching
        bool match = false;
        if (strcmp(handle->pattern, "*") == 0 || strcmp(handle->pattern, "*.*") == 0) {
            match = true;
        } else if (strcmp(handle->pattern, entry->d_name) == 0) {
            match = true;
        }

        if (match) {
            mbstowcs(lpFindFileData->cFileName, entry->d_name, 260);
            lpFindFileData->cFileName[259] = L'\0';

            char full_path[PATH_MAX];
            snprintf(full_path, sizeof(full_path), "%s/%s", handle->dirpath, entry->d_name);
            struct stat st;
            lpFindFileData->dwFileAttributes = 0;
            if (stat(full_path, &st) == 0) {
                if (S_ISDIR(st.st_mode)) {
                    lpFindFileData->dwFileAttributes |= 0x10;
                }
                lpFindFileData->nFileSizeLow = st.st_size & 0xFFFFFFFF;
                lpFindFileData->nFileSizeHigh = (st.st_size >> 32) & 0xFFFFFFFF;
            }

            lpFindFileData->cAlternateFileName[0] = L'\0';
            return 1;
        }
    }

    return 0;
}

inline int FindClose(HANDLE hFindFile) {
    if (hFindFile == INVALID_HANDLE_VALUE) return 1;

    _FindFileHandle* handle = (_FindFileHandle*)hFindFile;
    if (handle) {
        if (handle->dir) closedir(handle->dir);
        delete handle;
    }
    return 1;
}

// Delete file functions
inline int DeleteFileW(const wchar_t* lpFileName) {
    if (!lpFileName) return 0;

    char mb_path[PATH_MAX];
    size_t len = wcstombs(mb_path, lpFileName, sizeof(mb_path) - 1);
    if (len == (size_t)-1) return 0;
    mb_path[len] = '\0';

    return (unlink(mb_path) == 0) ? 1 : 0;
}

inline int DeleteFileA(const char* lpFileName) {
    if (!lpFileName) return 0;
    return (unlink(lpFileName) == 0) ? 1 : 0;
}

#endif // !_WIN32
