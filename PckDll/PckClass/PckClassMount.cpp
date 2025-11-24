//////////////////////////////////////////////////////////////////////
// PckClassMount.cpp: used to parse the data in the pck file of Perfect World Company and display it in the List
// Initialization of related classes, etc.
//
// This program is written by Li Qiufeng/stsm/liqf. The pck structure refers to Ruoshui's pck structure.txt, and
// Refer to the part of its Yi language code and read the pck file list
//
// This code is expected to be open source. Please retain the original author information for any modified release based on this code.
//
// 2015.5.27
//////////////////////////////////////////////////////////////////////
#include "PckClass.h"

BOOL CPckClass::MountPckFile(LPCWSTR	szFile)
{
	try
	{
		size_t versionCount = GetPckVersionCount();
		Logger.d("MountPckFile: Attempting to detect version, total versions: %zu", versionCount);

		for(size_t version = 0; version <= versionCount; version++)
		{
			Logger.d("MountPckFile: Trying version %zu of %zu", version, versionCount);
			if(DetectPckVerion(szFile, version))
			{
				Logger.d("MountPckFile: Version %zu detected, reading indexes", version);
				if(ReadPckFileIndexes())
				{
					//Set the entryType of the last Index to PCK_ENTRY_TYPE_TAIL_INDEX
					m_PckAllInfo.lpPckIndexTable[m_PckAllInfo.dwFileCount].entryType = PCK_ENTRY_TYPE_TAIL_INDEX;
					Logger.d("MountPckFile: Successfully mounted PCK file");
					return TRUE;
				}
			}
		}
		Logger.e("MountPckFile: Failed to detect any valid PCK version after trying %zu versions", versionCount + 1);
		return FALSE;
	}
	catch (MyException e) {
		Logger.e(e.what());
		return FALSE;
	}

}

void CPckClass::BuildDirTree()
{
	Logger.d("BuildDirTree: Starting directory tree construction");
	//Convert all ansi text in the read index to Unicode
	GenerateUnicodeStringToIndex();
	Logger.d("BuildDirTree: Unicode conversion complete");
	//Create a directory tree based on the file names in index
	ParseIndexTableToNode(m_PckAllInfo.lpPckIndexTable);
	Logger.d("BuildDirTree: Directory tree parsing complete");
}
