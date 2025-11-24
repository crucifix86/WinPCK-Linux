#pragma once

/*
	PCK version judgment, the code comes from BeySoft��s PckLib
*/
#pragma warning ( disable : 4309 )

#include <stdint.h>

class CPckAlgorithmId
{
public:
	CPckAlgorithmId(uint32_t id, uint32_t CustomPckGuardByte0 = 0, uint32_t CustomPckGuardByte1 = 0, uint32_t CustomPckMaskDword = 0, uint32_t CustomPckCheckMask = 0);
	~CPckAlgorithmId();

	uint32_t GetPckGuardByte0() { return PckGuardByte0; }
	uint32_t GetPckGuardByte1() { return PckGuardByte1; }
	uint32_t GetPckMaskDword() { return PckMaskDword; }
	uint32_t GetPckCheckMask() { return PckCheckMask; }

private:
	uint32_t  PckGuardByte0, PckGuardByte1, PckMaskDword, PckCheckMask;

	//void SetAlgorithmId(uint32_t id);
	void SetAlgorithmId(uint32_t id, uint32_t CustomPckGuardByte0 = 0, uint32_t CustomPckGuardByte1 = 0, uint32_t CustomPckMaskDword = 0, uint32_t CustomPckCheckMask = 0);

};

