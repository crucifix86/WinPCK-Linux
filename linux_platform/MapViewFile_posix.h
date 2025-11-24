#pragma once
/*
 * POSIX implementation of MapViewFile for Linux
 * This provides memory-mapped file I/O using mmap instead of Windows APIs
 */

#ifndef _WIN32

#include <stdint.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>
#include <assert.h>
#include <vector>
#include <string>
#include <cstring>
#include <wchar.h>
#include <stdexcept>
#include <stdio.h>
#include <stdlib.h>
#include <limits.h>

// Include platform definitions for consistent type definitions
#include "platform_defs.h"
#include "gccException.h"

// Windows memory mapping constants
#define FILE_MAP_READ    GENERIC_READ
#define FILE_MAP_WRITE   GENERIC_WRITE

// Windows types compatibility
// HANDLE is defined in fileapi_stubs.h as void*
typedef unsigned char BYTE;
typedef unsigned short WORD;
#ifndef DWORD
typedef uint32_t DWORD;
#endif
typedef long long LONGLONG;
typedef void* LPVOID;
typedef const void* LPCVOID;
typedef BYTE* LPBYTE;
typedef char* LPSTR;
typedef const char* LPCSTR;
typedef wchar_t* LPWSTR;
typedef const wchar_t* LPCWSTR;
typedef long LONG;
typedef unsigned int UINT;
typedef size_t SIZE_T;

#ifndef QWORD
typedef uint64_t QWORD;
#endif

#ifndef INVALID_HANDLE_VALUE
#define INVALID_HANDLE_VALUE ((HANDLE)(long long)-1)
#endif
#ifndef FALSE
#define FALSE 0
#endif
#ifndef TRUE
#define TRUE 1
#endif
#ifndef BOOL
#define BOOL int
#endif

// File access flags (Windows -> POSIX mapping)
#define GENERIC_READ 0x80000000L
#define GENERIC_WRITE 0x40000000L
#define FILE_SHARE_READ 0x00000001
#define FILE_SHARE_WRITE 0x00000002
#define CREATE_NEW 1
#define CREATE_ALWAYS 2
#define OPEN_EXISTING 3
#define OPEN_ALWAYS 4
#define TRUNCATE_EXISTING 5
#define FILE_ATTRIBUTE_NORMAL 0x80
#define FILE_ATTRIBUTE_DIRECTORY 0x10
#define INVALID_FILE_ATTRIBUTES 0xFFFFFFFF
#define FILE_BEGIN 0
#define FILE_CURRENT 1
#define FILE_END 2

// Path separator
#define PATH_SEPERATOR "/"
#define MAX_PATH_LEN PATH_MAX

// Helper union for QWORD
typedef union _UNQWORD {
    QWORD qwValue;
    LONGLONG llwValue;
    struct {
        DWORD dwValue;
        DWORD dwValueHigh;
    };
    struct {
        LONG lValue;
        LONG lValueHigh;
    };
} UNQWORD, *LPUNQWORD;

class CMapViewFile {
public:
    CMapViewFile();
    virtual ~CMapViewFile();

    BOOL FileExists(LPCSTR szName);
    BOOL FileExists(LPCWSTR szName);

    BOOL Open(LPCSTR lpszFilename, DWORD dwDesiredAccess, DWORD dwShareMode,
              DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes);
    BOOL Open(LPCWSTR lpszFilename, DWORD dwDesiredAccess, DWORD dwShareMode,
              DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes);

    void SetFilePointer(QWORD lDistanceToMove, DWORD dwMoveMethod = FILE_BEGIN);
    QWORD GetFilePointer();
    DWORD Read(LPVOID buffer, DWORD dwBytesToRead);
    QWORD GetFileSize();

    virtual LPBYTE View(QWORD dwAddress, DWORD dwSize);
    void UnmapView(LPVOID lpTargetAddress);
    void UnmapViewAll();
    void UnMaping();
    void clear();

    const char* GetFileDiskName();
    virtual BOOL FlushFileBuffers() { throw std::runtime_error("program cannot reach here"); }

protected:
    LPCSTR GenerateMapName();
    void MakeUnlimitedPath(LPWSTR _dst, LPCWSTR _src, size_t size);
    void MakeUnlimitedPath(LPSTR _dst, LPCSTR _src, size_t size);

    template <typename T>
    void GetDiskNameFromFilename(T* lpszFilename);

    uint8_t* ViewReal(QWORD qwAddress, DWORD dwSize, DWORD dwDesiredAccess);

protected:
    HANDLE hFile;
    HANDLE hFileMapping;  // Not used in POSIX, kept for compatibility
    std::vector<void*> vMapAddress;
    char m_szDisk[8];
    char szFileMappingName[32];
    QWORD fileSize;
    DWORD accessMode;

private:
    virtual void SetSparseFile() { throw std::runtime_error("program cannot reach here"); }
};

class CMapViewFileRead : public CMapViewFile {
public:
    CMapViewFileRead();
    virtual ~CMapViewFileRead();

    BOOL Open(LPCSTR lpszFilename);
    BOOL Open(LPCWSTR lpszFilename);
    BOOL Mapping();
    LPBYTE View(QWORD dwAddress, DWORD dwSize);
    virtual LPBYTE ReView(LPVOID lpMapAddressOld, QWORD dwAddress, DWORD dwSize);
    BOOL OpenMappingRead(LPCSTR lpFileName);
    BOOL OpenMappingRead(LPCWSTR lpFileName);
    LPBYTE OpenMappingViewAllRead(LPCSTR lpFileName);
    LPBYTE OpenMappingViewAllRead(LPCWSTR lpFileName);
};

class CMapViewFileWrite : public CMapViewFile {
public:
    CMapViewFileWrite();
    virtual ~CMapViewFileWrite();

    BOOL Open(LPCSTR lpszFilename, DWORD dwCreationDisposition, BOOL isNTFSSparseFile = FALSE);
    BOOL Open(LPCWSTR lpszFilename, DWORD dwCreationDisposition, BOOL isNTFSSparseFile = FALSE);
    BOOL Mapping(QWORD dwMaxSize);
    LPBYTE View(QWORD dwAddress, DWORD dwSize);
    virtual LPBYTE ReView(LPVOID lpMapAddressOld, QWORD dwAddress, DWORD dwSize);
    BOOL SetEndOfFile();
    DWORD Write(LPVOID buffer, DWORD dwBytesToWrite);
    BOOL OpenMappingWrite(LPCSTR lpFileName, DWORD dwCreationDisposition, QWORD qdwSizeToMap);
    BOOL OpenMappingWrite(LPCWSTR lpFileName, DWORD dwCreationDisposition, QWORD qdwSizeToMap);
    virtual BOOL FlushFileBuffers();

private:
    virtual void SetSparseFile();
    QWORD maxMappedSize;
};

#endif // !_WIN32
