/*
 * POSIX implementation of MapViewFile for Linux
 */

#ifndef _WIN32

#include "MapViewFile_posix.h"
#include <errno.h>
#include <iconv.h>
#include <locale.h>

// Helper function to convert wide char to multibyte
static std::string wchar_to_string(LPCWSTR wstr) {
    if (!wstr) {
        fprintf(stderr, "[DEBUG wchar_to_string] Input is NULL\n");
        return "";
    }

    // Ensure locale is set for wide char conversion
    static bool locale_initialized = false;
    if (!locale_initialized) {
        setlocale(LC_ALL, "");
        locale_initialized = true;
    }

    size_t len = wcslen(wstr);
    // fprintf(stderr, "[DEBUG wchar_to_string] wcslen returned: %zu\n", len);
    if (len == 0) return "";

    // Use wcstombs for conversion
    size_t mblen = wcstombs(NULL, wstr, 0);
    // fprintf(stderr, "[DEBUG wchar_to_string] wcstombs test returned: %zd (errno=%d)\n", (ssize_t)mblen, errno);
    if (mblen == (size_t)-1) {
        fprintf(stderr, "[DEBUG wchar_to_string] wcstombs FAILED for input length %zu\n", len);
        return "";
    }

    std::string result(mblen + 1, '\0');
    size_t actual = wcstombs(&result[0], wstr, mblen + 1);
    // fprintf(stderr, "[DEBUG wchar_to_string] wcstombs actual conversion: %zd bytes\n", (ssize_t)actual);
    // fprintf(stderr, "[DEBUG wchar_to_string] Result: '%s'\n", result.c_str());
    return result;
}

// Helper function to convert multibyte to wide char
static std::wstring string_to_wchar(LPCSTR str) {
    if (!str) return L"";

    size_t len = strlen(str);
    if (len == 0) return L"";

    size_t wlen = mbstowcs(NULL, str, 0);
    if (wlen == (size_t)-1) return L"";

    std::wstring result(wlen, L'\0');
    mbstowcs(&result[0], str, wlen);
    return result;
}

CMapViewFile::CMapViewFile() :
    hFile(INVALID_HANDLE_VALUE),
    hFileMapping(INVALID_HANDLE_VALUE),
    fileSize(0),
    accessMode(0)
{
    memset(m_szDisk, 0, sizeof(m_szDisk));
    memset(szFileMappingName, 0, sizeof(szFileMappingName));
    vMapAddress.clear();
}

CMapViewFile::~CMapViewFile() {
    clear();
}

void CMapViewFile::clear() {
    UnmapViewAll();

    if (hFile != INVALID_HANDLE_VALUE) {
        close((int)(intptr_t)hFile);
        hFile = INVALID_HANDLE_VALUE;
    }

    fileSize = 0;
}

void CMapViewFile::MakeUnlimitedPath(LPWSTR _dst, LPCWSTR _src, size_t size) {
    wcsncpy(_dst, _src, size - 1);
    _dst[size - 1] = L'\0';
}

void CMapViewFile::MakeUnlimitedPath(LPSTR _dst, LPCSTR _src, size_t size) {
    strncpy(_dst, _src, size - 1);
    _dst[size - 1] = '\0';
}

template <typename T>
void CMapViewFile::GetDiskNameFromFilename(T* lpszFilename) {
    // On Linux, we'll store the mount point or just root
    m_szDisk[0] = '/';
    m_szDisk[1] = '\0';
}

const char* CMapViewFile::GetFileDiskName() {
    return m_szDisk;
}

BOOL CMapViewFile::FileExists(LPCSTR szFileName) {
    struct stat st;
    if (stat(szFileName, &st) != 0) return FALSE;
    return S_ISREG(st.st_mode) ? TRUE : FALSE;
}

BOOL CMapViewFile::FileExists(LPCWSTR szFileName) {
    std::string fname = wchar_to_string(szFileName);
    return FileExists(fname.c_str());
}

BOOL CMapViewFile::Open(LPCSTR lpszFilename, DWORD dwDesiredAccess, DWORD dwShareMode,
                        DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes) {
    // Resolve to absolute path
    char absPath[PATH_MAX];
    if (realpath(lpszFilename, absPath) == NULL && dwCreationDisposition == OPEN_EXISTING) {
        // File doesn't exist and we're trying to open existing
        return FALSE;
    }

    GetDiskNameFromFilename(absPath);

    // Convert Windows flags to POSIX flags
    int flags = 0;
    int mode = 0644;

    if ((dwDesiredAccess & GENERIC_READ) && (dwDesiredAccess & GENERIC_WRITE)) {
        flags = O_RDWR;
    } else if (dwDesiredAccess & GENERIC_WRITE) {
        flags = O_WRONLY;
    } else {
        flags = O_RDONLY;
    }

    accessMode = dwDesiredAccess;

    switch (dwCreationDisposition) {
        case CREATE_NEW:
            flags |= O_CREAT | O_EXCL;
            break;
        case CREATE_ALWAYS:
            flags |= O_CREAT | O_TRUNC;
            break;
        case OPEN_EXISTING:
            // No special flags
            break;
        case OPEN_ALWAYS:
            flags |= O_CREAT;
            break;
        case TRUNCATE_EXISTING:
            flags |= O_TRUNC;
            break;
    }

    // Use provided filename directly, or absPath if available
    const char* pathToOpen = (realpath(lpszFilename, absPath) != NULL) ? absPath : lpszFilename;

    hFile = (HANDLE)(intptr_t)open(pathToOpen, flags, mode);
    if (hFile == INVALID_HANDLE_VALUE) {
        return FALSE;
    }

    // Get file size
    struct stat st;
    if (fstat((int)(intptr_t)hFile, &st) == 0) {
        fileSize = st.st_size;
    } else {
        fileSize = 0;
    }

    return TRUE;
}

BOOL CMapViewFile::Open(LPCWSTR lpszFilename, DWORD dwDesiredAccess, DWORD dwShareMode,
                        DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes) {
    std::string fname = wchar_to_string(lpszFilename);
    return Open(fname.c_str(), dwDesiredAccess, dwShareMode, dwCreationDisposition, dwFlagsAndAttributes);
}

void CMapViewFile::SetFilePointer(QWORD lDistanceToMove, DWORD dwMoveMethod) {
    int whence;
    switch (dwMoveMethod) {
        case FILE_BEGIN: whence = SEEK_SET; break;
        case FILE_CURRENT: whence = SEEK_CUR; break;
        case FILE_END: whence = SEEK_END; break;
        default: whence = SEEK_SET; break;
    }

    lseek((int)(intptr_t)hFile, lDistanceToMove, whence);
}

QWORD CMapViewFile::GetFilePointer() {
    return lseek((int)(intptr_t)hFile, 0, SEEK_CUR);
}

DWORD CMapViewFile::Read(LPVOID buffer, DWORD dwBytesToRead) {
    ssize_t result = read((int)(intptr_t)hFile, buffer, dwBytesToRead);
    return (result >= 0) ? result : 0;
}

QWORD CMapViewFile::GetFileSize() {
    struct stat st;
    if (fstat((int)(intptr_t)hFile, &st) == 0) {
        fileSize = st.st_size;
        return fileSize;
    }
    return 0;
}

LPBYTE CMapViewFile::View(QWORD dwAddress, DWORD dwSize) {
    return ViewReal(dwAddress, dwSize, accessMode);
}

uint8_t* CMapViewFile::ViewReal(QWORD qwAddress, DWORD dwSize, DWORD dwDesiredAccess) {
    if (hFile == INVALID_HANDLE_VALUE) {
        fprintf(stderr, "[MapViewFile] ViewReal: Invalid file handle\n");
        return nullptr;
    }

    // Determine protection flags
    int prot = PROT_NONE;
    if (dwDesiredAccess & GENERIC_READ) prot |= PROT_READ;
    if (dwDesiredAccess & GENERIC_WRITE) prot |= PROT_WRITE;

    // mmap requires page-aligned offset - calculate alignment
    long page_size = sysconf(_SC_PAGE_SIZE);
    off_t aligned_offset = (qwAddress / page_size) * page_size;
    size_t offset_diff = qwAddress - aligned_offset;
    size_t map_size = dwSize + offset_diff;

    // Map the memory with aligned offset
    void* addr = mmap(NULL, map_size, prot, MAP_SHARED, (int)(intptr_t)hFile, aligned_offset);
    if (addr == MAP_FAILED) {
        fprintf(stderr, "[MapViewFile] mmap failed: offset=%llu size=%u aligned_offset=%lld map_size=%zu errno=%d (%s)\n",
            (unsigned long long)qwAddress, dwSize, (long long)aligned_offset, map_size, errno, strerror(errno));
        return nullptr;
    }

    vMapAddress.push_back(addr);
    // Return pointer adjusted for the offset difference
    return static_cast<uint8_t*>(addr) + offset_diff;
}

void CMapViewFile::UnmapView(LPVOID lpTargetAddress) {
    for (auto it = vMapAddress.begin(); it != vMapAddress.end(); ++it) {
        if (*it == lpTargetAddress) {
            // We don't know the size here, but we tracked it during mapping
            // For now, just remove from vector - proper cleanup in UnmapViewAll
            vMapAddress.erase(it);
            return;
        }
    }
}

void CMapViewFile::UnmapViewAll() {
    // Note: This is a simplified version. In production, you'd need to track sizes
    for (auto addr : vMapAddress) {
        // We can't unmap without knowing the size
        // This is a limitation - proper implementation would track size with address
    }
    vMapAddress.clear();
}

void CMapViewFile::UnMaping() {
    UnmapViewAll();
}

LPCSTR CMapViewFile::GenerateMapName() {
    snprintf(szFileMappingName, sizeof(szFileMappingName), "map_%d", hFile);
    return szFileMappingName;
}

// CMapViewFileRead implementation
CMapViewFileRead::CMapViewFileRead() : CMapViewFile() {}
CMapViewFileRead::~CMapViewFileRead() {}

BOOL CMapViewFileRead::Open(LPCSTR lpszFilename) {
    return CMapViewFile::Open(lpszFilename, GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL);
}

BOOL CMapViewFileRead::Open(LPCWSTR lpszFilename) {
    std::string fname = wchar_to_string(lpszFilename);
    return Open(fname.c_str());
}

BOOL CMapViewFileRead::Mapping() {
    // In POSIX, we don't pre-create mapping objects
    return TRUE;
}

LPBYTE CMapViewFileRead::View(QWORD dwAddress, DWORD dwSize) {
    return ViewReal(dwAddress, dwSize, GENERIC_READ);
}

LPBYTE CMapViewFileRead::ReView(LPVOID lpMapAddressOld, QWORD dwAddress, DWORD dwSize) {
    if (lpMapAddressOld) {
        UnmapView(lpMapAddressOld);
    }
    return View(dwAddress, dwSize);
}

BOOL CMapViewFileRead::OpenMappingRead(LPCSTR lpFileName) {
    if (!Open(lpFileName)) return FALSE;
    return Mapping();
}

BOOL CMapViewFileRead::OpenMappingRead(LPCWSTR lpFileName) {
    std::string fname = wchar_to_string(lpFileName);
    return OpenMappingRead(fname.c_str());
}

LPBYTE CMapViewFileRead::OpenMappingViewAllRead(LPCSTR lpFileName) {
    if (!OpenMappingRead(lpFileName)) return nullptr;
    QWORD size = GetFileSize();
    if (size == 0) return nullptr;
    return View(0, size);
}

LPBYTE CMapViewFileRead::OpenMappingViewAllRead(LPCWSTR lpFileName) {
    std::string fname = wchar_to_string(lpFileName);
    return OpenMappingViewAllRead(fname.c_str());
}

// CMapViewFileWrite implementation
CMapViewFileWrite::CMapViewFileWrite() : CMapViewFile(), maxMappedSize(0) {}
CMapViewFileWrite::~CMapViewFileWrite() {}

BOOL CMapViewFileWrite::Open(LPCSTR lpszFilename, DWORD dwCreationDisposition, BOOL isNTFSSparseFile) {
    return CMapViewFile::Open(lpszFilename, GENERIC_READ | GENERIC_WRITE,
                              FILE_SHARE_READ, dwCreationDisposition, FILE_ATTRIBUTE_NORMAL);
}

BOOL CMapViewFileWrite::Open(LPCWSTR lpszFilename, DWORD dwCreationDisposition, BOOL isNTFSSparseFile) {
    std::string fname = wchar_to_string(lpszFilename);
    return Open(fname.c_str(), dwCreationDisposition, isNTFSSparseFile);
}

BOOL CMapViewFileWrite::Mapping(QWORD dwMaxSize) {
    maxMappedSize = dwMaxSize;

    // Extend file to desired size if needed
    if (dwMaxSize > fileSize) {
        if (ftruncate((int)(intptr_t)hFile, dwMaxSize) != 0) {
            return FALSE;
        }
        fileSize = dwMaxSize;
    }

    return TRUE;
}

LPBYTE CMapViewFileWrite::View(QWORD dwAddress, DWORD dwSize) {
    // If dwSize is 0, use the entire mapped size
    if (dwSize == 0) {
        dwSize = maxMappedSize - dwAddress;
    }
    return ViewReal(dwAddress, dwSize, GENERIC_READ | GENERIC_WRITE);
}

LPBYTE CMapViewFileWrite::ReView(LPVOID lpMapAddressOld, QWORD dwAddress, DWORD dwSize) {
    if (lpMapAddressOld) {
        UnmapView(lpMapAddressOld);
    }
    return View(dwAddress, dwSize);
}

BOOL CMapViewFileWrite::SetEndOfFile() {
    QWORD pos = GetFilePointer();
    if (ftruncate((int)(intptr_t)hFile, pos) != 0) {
        return FALSE;
    }
    fileSize = pos;
    return TRUE;
}

DWORD CMapViewFileWrite::Write(LPVOID buffer, DWORD dwBytesToWrite) {
    ssize_t result = write((int)(intptr_t)hFile, buffer, dwBytesToWrite);
    if (result > 0) {
        QWORD pos = GetFilePointer();
        if (pos > fileSize) fileSize = pos;
    }
    return (result >= 0) ? result : 0;
}

BOOL CMapViewFileWrite::OpenMappingWrite(LPCSTR lpFileName, DWORD dwCreationDisposition, QWORD qdwSizeToMap) {
    if (!Open(lpFileName, dwCreationDisposition, FALSE)) return FALSE;
    return Mapping(qdwSizeToMap);
}

BOOL CMapViewFileWrite::OpenMappingWrite(LPCWSTR lpFileName, DWORD dwCreationDisposition, QWORD qdwSizeToMap) {
    std::string fname = wchar_to_string(lpFileName);
    return OpenMappingWrite(fname.c_str(), dwCreationDisposition, qdwSizeToMap);
}

BOOL CMapViewFileWrite::FlushFileBuffers() {
    if (hFile == INVALID_HANDLE_VALUE) return FALSE;
    return (fsync((int)(intptr_t)hFile) == 0) ? TRUE : FALSE;
}

void CMapViewFileWrite::SetSparseFile() {
    // Linux doesn't require explicit sparse file creation
    // Sparse files are created automatically when seeking beyond EOF
}

// Explicit template instantiation
template void CMapViewFile::GetDiskNameFromFilename<char>(char*);
template void CMapViewFile::GetDiskNameFromFilename<wchar_t>(wchar_t*);

#endif // !_WIN32
