cmake_minimum_required(VERSION 3.10)
project(WinPCK_Linux VERSION 1.33.1 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 11)

# Platform-specific settings
if(UNIX AND NOT APPLE)
    add_definitions(-D_FILE_OFFSET_BITS=64)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -Wno-narrowing")
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fPIC")
endif()

# Include directories
include_directories(
    ${CMAKE_SOURCE_DIR}/PckDll/include
    ${CMAKE_SOURCE_DIR}/PckDll/PckClass
    ${CMAKE_SOURCE_DIR}/PckDll/MapViewFile
    ${CMAKE_SOURCE_DIR}/PckDll/DictHash
    ${CMAKE_SOURCE_DIR}/PckDll/PckControlCenter
    ${CMAKE_SOURCE_DIR}/PckDll/ZupClass
    ${CMAKE_SOURCE_DIR}/zlib
    ${CMAKE_SOURCE_DIR}/libdeflate
    ${CMAKE_SOURCE_DIR}/base64
    ${CMAKE_SOURCE_DIR}/tlib
    ${CMAKE_SOURCE_DIR}/MiscFuncs
    ${CMAKE_SOURCE_DIR}/linux_platform
)

# Build zlib
file(GLOB ZLIB_SOURCES
    zlib/adler32.c
    zlib/compress.c
    zlib/crc32.c
    zlib/deflate.c
    zlib/infback.c
    zlib/inffast.c
    zlib/inflate.c
    zlib/inftrees.c
    zlib/trees.c
    zlib/uncompr.c
    zlib/zutil.c
)
add_library(zlib_internal STATIC ${ZLIB_SOURCES})

# Build libdeflate
file(GLOB LIBDEFLATE_SOURCES
    libdeflate/lib/*.c
    libdeflate/lib/x86/*.c
    libdeflate/lib/arm/*.c
)
add_library(libdeflate_internal STATIC ${LIBDEFLATE_SOURCES})
target_include_directories(libdeflate_internal PRIVATE
    ${CMAKE_SOURCE_DIR}/libdeflate
    ${CMAKE_SOURCE_DIR}/libdeflate/common
)

# Build base64
file(GLOB BASE64_SOURCES base64/*.c base64/*.cpp)
if(BASE64_SOURCES)
    add_library(base64_internal STATIC ${BASE64_SOURCES})
endif()

# PckDll library sources
file(GLOB PCK_CLASS_SOURCES PckDll/PckClass/*.cpp)
file(GLOB PCK_DICT_SOURCES PckDll/DictHash/*.cpp)
file(GLOB PCK_CONTROL_SOURCES PckDll/PckControlCenter/*.cpp)
file(GLOB PCK_ZUP_SOURCES PckDll/ZupClass/*.cpp)
file(GLOB PCK_SRC_SOURCES PckDll/src/*.cpp)
# MiscFuncs sources - only include non-Windows-UI files
set(MISC_FUNCS_SOURCES
    MiscFuncs/AllocMemPool.cpp
    MiscFuncs/CharsCodeConv.cpp
    MiscFuncs/Raw2HexString.cpp
    MiscFuncs/TextLineSpliter.cpp
)

# Platform-specific MapViewFile sources
if(WIN32)
    file(GLOB PCK_MAPVIEW_SOURCES PckDll/MapViewFile/*.cpp)
else()
    # Use POSIX implementation + Windows Multi* classes for Linux
    set(PCK_MAPVIEW_SOURCES
        linux_platform/MapViewFile_posix.cpp
        PckDll/MapViewFile/MapViewFileMulti.cpp
        PckDll/MapViewFile/MapViewFileMultiRead.cpp
        PckDll/MapViewFile/MapViewFileMultiWrite.cpp
        PckDll/MapViewFile/MapViewFileMultiPck.cpp
        PckDll/MapViewFile/MapViewFileMultiPckRead.cpp
        PckDll/MapViewFile/MapViewFileMultiPckWrite.cpp
    )
endif()

# Combine all PCK sources
set(PCK_LIBRARY_SOURCES
    ${PCK_CLASS_SOURCES}
    ${PCK_MAPVIEW_SOURCES}
    ${PCK_DICT_SOURCES}
    ${PCK_CONTROL_SOURCES}
    ${PCK_ZUP_SOURCES}
    ${PCK_SRC_SOURCES}
    ${MISC_FUNCS_SOURCES}
)

# Build PCK library as static library
add_library(pcklib STATIC ${PCK_LIBRARY_SOURCES})
target_link_libraries(pcklib
    zlib_internal
    libdeflate_internal
)
if(TARGET base64_internal)
    target_link_libraries(pcklib base64_internal)
endif()

# Link pthread for Linux
if(UNIX)
    target_link_libraries(pcklib pthread)
endif()

# CLI tool (to be created)
add_executable(pck_cli
    linux_cli/main.cpp
)
target_link_libraries(pck_cli pcklib)
