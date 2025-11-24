#include "PckAlgorithmId.h"
#include "pck_default_vars.h"
#if PCK_DEBUG_OUTPUT
#include "PckClassLog.h"
#endif

CPckAlgorithmId::CPckAlgorithmId(uint32_t id, uint32_t CustomPckGuardByte0, uint32_t CustomPckGuardByte1, uint32_t CustomPckMaskDword, uint32_t CustomPckCheckMask)
{
	SetAlgorithmId(id, CustomPckGuardByte0, CustomPckGuardByte1, CustomPckMaskDword, CustomPckCheckMask);
}


CPckAlgorithmId::~CPckAlgorithmId()
{}

void CPckAlgorithmId::SetAlgorithmId(uint32_t id, uint32_t CustomPckGuardByte0, uint32_t CustomPckGuardByte1, uint32_t CustomPckMaskDword, uint32_t CustomPckCheckMask)
{
	//0 Jade Dynasty, Perfect World
	//111 Hot Dance Party
	//121 Ether Saga Odyssey
	//131 Forsaken World
	//161 Saint Seiya, Swordsman Online

	switch (id)
	{
	case 111:
		PckGuardByte0 = 0xAB12908F;
		PckGuardByte1 = 0xB3231902;
		PckMaskDword = 0x2A63810E;
		PckCheckMask = 0x18734563;
		break;

	default:
		PckGuardByte0 = 0xFDFDFEEE + id * 0x72341F2;
		PckGuardByte1 = 0xF00DBEEF + id * 0x1237A73;
		//oficial
		PckMaskDword = 0xA8937462 + id * 0xAB2321F; //key1
		PckCheckMask = 0x59374231 + id * 0x987A223; //key2
		break;
	}

	if(CustomPckMaskDword != 0)
		PckMaskDword = CustomPckMaskDword;

	if(CustomPckCheckMask != 0)	
		PckCheckMask = CustomPckCheckMask;

	if (CustomPckGuardByte0 != 0)
		PckGuardByte0 = CustomPckGuardByte0;

	if (CustomPckGuardByte1 != 0)
		PckGuardByte1 = CustomPckGuardByte1;
}