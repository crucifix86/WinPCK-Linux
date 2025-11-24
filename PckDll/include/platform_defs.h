#pragma once
/*
 * Cross-platform type definitions
 * Maps Microsoft-specific types to standard C types
 */

#ifndef _PLATFORM_DEFS_H_
#define _PLATFORM_DEFS_H_

#include <stdint.h>

#ifndef _WIN32
#include "winapi_stubs.h"
#include "fileapi_stubs.h"
#endif

// Microsoft-specific integer types
// We need to handle "unsigned __int32" syntax, so we can't use simple #define
#ifndef _WIN32
    typedef int8_t __int8;
    typedef int16_t __int16;
    typedef int32_t __int32;
    typedef int64_t __int64;

    // Additional Windows types
    typedef unsigned int UINT;
    #ifndef DWORD
    typedef uint32_t DWORD;
    #endif
    typedef size_t SIZE_T;
    typedef void VOID;
    typedef wchar_t TCHAR;
    typedef wchar_t WCHAR;
    typedef char CHAR;

    // ULARGE_INTEGER structure (may be defined in fileapi_stubs.h)
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

    // Code page constants
    #define CP_ACP 0  // ANSI code page

    // File attributes
    #define FILE_ATTRIBUTE_DIRECTORY 0x10

    // Path constants
    #ifndef MAX_PATH
    #define MAX_PATH 260
    #endif

    // Microsoft-specific keywords
    #define __forceinline inline __attribute__((always_inline))
    #define _inline inline
    #define __fastcall  // GCC uses register calling convention by default
    #define CONST const

    // String functions - handle both 2-arg and 3-arg versions
    // Helper to get second argument (for when size is provided as arg1)
    #define _GET_2ND_ARG(arg1, arg2, ...) arg2
    #define _GET_1ST_OR_2ND(...) _GET_2ND_ARG(__VA_ARGS__, __VA_ARGS__, )

    // We use variadic macros to handle the optional size parameter
    #define wcscpy_s(dest, ...) wcscpy(dest, _GET_1ST_OR_2ND(__VA_ARGS__))
    #define wcscat_s(dest, ...) wcscat(dest, _GET_1ST_OR_2ND(__VA_ARGS__))
    #define strcpy_s(dest, ...) strcpy(dest, _GET_1ST_OR_2ND(__VA_ARGS__))
    #define strcat_s(dest, ...) strcat(dest, _GET_1ST_OR_2ND(__VA_ARGS__))
    #define memmove_s(dest, destsz, src, count) memmove(dest, src, count)

    // Printf functions
    #define _vsnprintf vsnprintf
    #define _vsnwprintf vswprintf
    // sprintf_s and swprintf_s: if 2+ args after dest, assume first is size (skip it)
    #define sprintf_s(dest, ...) sprintf(dest, __VA_ARGS__)
    #define swprintf_s(dest, size, ...) swprintf(dest, size, __VA_ARGS__)

    // String comparison (case insensitive)
    #include <strings.h>
    #include <ctype.h>
    #define wcsicmp wcscasecmp

    // String case conversion
    inline char* strlwr(char* str) {
        for (char* p = str; *p; ++p) *p = tolower(*p);
        return str;
    }

    // Byte swap functions
    #include <byteswap.h>
    #define _byteswap_ushort(x) __bswap_16(x)
    #define _byteswap_ulong(x) __bswap_32(x)
    #define _byteswap_uint64(x) __bswap_64(x)

    // Error handling
    #include <errno.h>
    inline uint32_t GetLastError() { return errno; }
    #ifndef NOERROR
    #define NOERROR 0
    #endif
#endif

#endif // _PLATFORM_DEFS_H_
