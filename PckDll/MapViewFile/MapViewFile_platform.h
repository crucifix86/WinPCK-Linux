#pragma once
/*
 * Platform-specific header for MapViewFile
 * Includes appropriate implementation based on platform
 */

#ifdef _WIN32
    // Windows implementation
    #include "MapViewFile.h"
#else
    // Linux/POSIX implementation
    #include "MapViewFile_posix.h"
#endif
