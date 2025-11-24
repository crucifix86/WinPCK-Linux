#pragma once
/*
 * Stubs for Windows API functions not available on Linux
 */

#ifndef _WIN32

#include <wchar.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <libgen.h>
#include <unistd.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <time.h>
#include <errno.h>
#include <stdarg.h>

// GetFileTitleW - extracts filename from path
inline int GetFileTitleW(const wchar_t* lpszFile, wchar_t* lpszTitle, unsigned short cbBuf) {
    if (!lpszFile || !lpszTitle || cbBuf == 0) return -1;

    // Convert wide string to multibyte for basename
    char mb_path[4096];
    size_t len = wcstombs(mb_path, lpszFile, sizeof(mb_path) - 1);
    if (len == (size_t)-1) return -1;
    mb_path[len] = '\0';

    // Get basename
    char* base = basename(mb_path);

    // Convert back to wide string
    size_t wlen = mbstowcs(lpszTitle, base, cbBuf - 1);
    if (wlen == (size_t)-1) return -1;
    lpszTitle[wlen] = L'\0';

    return 0;
}

// OutputDebugStringA - prints debug string (stub for Linux)
inline void OutputDebugStringA(const char* lpOutputString) {
    // On Linux, just write to stderr
    if (lpOutputString) {
        fprintf(stderr, "[DEBUG] %s", lpOutputString);
    }
}

// Directory functions
inline int CreateDirectoryW(const wchar_t* lpPathName, void* lpSecurityAttributes) {
    if (!lpPathName) return 0;

    // Convert wide string to multibyte
    char mb_path[4096];
    size_t len = wcstombs(mb_path, lpPathName, sizeof(mb_path) - 1);
    if (len == (size_t)-1) return 0;
    mb_path[len] = '\0';

    // Create directory with default permissions
    return (mkdir(mb_path, 0755) == 0) ? 1 : 0;
}

inline int CreateDirectoryA(const char* lpPathName, void* lpSecurityAttributes) {
    if (!lpPathName) return 0;
    return (mkdir(lpPathName, 0755) == 0) ? 1 : 0;
}

inline int SetCurrentDirectoryW(const wchar_t* lpPathName) {
    if (!lpPathName) return 0;

    // Convert wide string to multibyte
    char mb_path[4096];
    size_t len = wcstombs(mb_path, lpPathName, sizeof(mb_path) - 1);
    if (len == (size_t)-1) return 0;
    mb_path[len] = '\0';

    return (chdir(mb_path) == 0) ? 1 : 0;
}

inline int SetCurrentDirectoryA(const char* lpPathName) {
    if (!lpPathName) return 0;
    return (chdir(lpPathName) == 0) ? 1 : 0;
}

// TEXT macro for string literals
#ifndef TEXT
#define TEXT(x) L##x
#endif

// PathRemoveFileSpecW - removes the file spec from path (leaves directory)
inline int PathRemoveFileSpecW(wchar_t* pszPath) {
    if (!pszPath) return 0;

    // Find last backslash or forward slash
    wchar_t* lastSlash = wcsrchr(pszPath, L'\\');
    wchar_t* lastFSlash = wcsrchr(pszPath, L'/');

    wchar_t* cutPoint = nullptr;
    if (lastSlash && lastFSlash) {
        cutPoint = (lastSlash > lastFSlash) ? lastSlash : lastFSlash;
    } else if (lastSlash) {
        cutPoint = lastSlash;
    } else if (lastFSlash) {
        cutPoint = lastFSlash;
    }

    if (cutPoint) {
        *cutPoint = L'\0';
        return 1;
    }
    return 0;
}

// FormatMessageW - stub for Linux (simplified error message formatting)
#define FORMAT_MESSAGE_FROM_SYSTEM 0x00001000
#define FORMAT_MESSAGE_IGNORE_INSERTS 0x00000200
#define LANG_NEUTRAL 0x00
#define SUBLANG_DEFAULT 0x01
#define MAKELANGID(p, s) ((((unsigned short)(s)) << 10) | (unsigned short)(p))

inline int FormatMessageW(unsigned long dwFlags, const void* lpSource, unsigned long dwMessageId,
                          unsigned long dwLanguageId, wchar_t* lpBuffer, unsigned long nSize, va_list* Arguments) {
    // Simplified stub - just convert errno to string
    char* err_str = strerror(dwMessageId);
    if (err_str) {
        mbstowcs(lpBuffer, err_str, nSize);
        return wcslen(lpBuffer);
    }
    return 0;
}

// SetLastError - stub for Linux
inline void SetLastError(unsigned long dwErrCode) {
    errno = dwErrCode;
}

// SYSTEMTIME structure
typedef struct _SYSTEMTIME {
    unsigned short wYear;
    unsigned short wMonth;
    unsigned short wDayOfWeek;
    unsigned short wDay;
    unsigned short wHour;
    unsigned short wMinute;
    unsigned short wSecond;
    unsigned short wMilliseconds;
} SYSTEMTIME;

// GetLocalTime - stub for Linux
inline void GetLocalTime(SYSTEMTIME* lpSystemTime) {
    time_t now = time(nullptr);
    struct tm* local = localtime(&now);
    if (local && lpSystemTime) {
        lpSystemTime->wYear = local->tm_year + 1900;
        lpSystemTime->wMonth = local->tm_mon + 1;
        lpSystemTime->wDayOfWeek = local->tm_wday;
        lpSystemTime->wDay = local->tm_mday;
        lpSystemTime->wHour = local->tm_hour;
        lpSystemTime->wMinute = local->tm_min;
        lpSystemTime->wSecond = local->tm_sec;
        lpSystemTime->wMilliseconds = 0;
    }
}

// Console functions - stubs for Linux
inline unsigned long GetConsoleTitleW(wchar_t* lpConsoleTitle, unsigned long nSize) {
    wcscpy(lpConsoleTitle, L"WinPCK");
    return 6;
}

inline int SetConsoleTitleW(const wchar_t* lpConsoleTitle) {
    return 1;
}

inline int SetConsoleTitleA(const char* lpConsoleTitle) {
    return 1;
}

#endif // !_WIN32
