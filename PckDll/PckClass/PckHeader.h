//////////////////////////////////////////////////////////////////////
// PckHeader.h: used to parse the data in the pck file of Perfect World Company and display it in the List
// head File
//
// This program is written by Li Qiufeng/stsm/liqf. The pck structure refers to Ruoshui's pck structure.txt, and
// Refer to the part of its Yi language code and read the pck file list
//
// This code is expected to be open source. Please retain the original author information for any modified release based on this code.
//
// 2012.4.10
//////////////////////////////////////////////////////////////////////

#if !defined(_PCKHEADER_H_)
#define _PCKHEADER_H_

// Only enforce UNICODE on Windows
#if defined(_WIN32) && !defined(UNICODE)
#error("please use Unicode charset")
#endif

// On Linux, we always use Unicode (wchar_t)
#ifndef _WIN32
#define UNICODE
#endif

#include "PckStructs.h"

#endif