#include <stdio.h>
#include <wchar.h>
#include "PckDll/include/pck_handle.h"

void test_log_callback(const char level, const wchar_t *str) {
    wprintf(L"[%c] %ls\n", level, str);
}

int main() {
    log_regShowFunc(test_log_callback);

    const wchar_t* filepath = L"/home/doug/Desktop/PW153EN/PWI_EN/element/configs.pck";
    wprintf(L"Attempting to open: %ls\n", filepath);

    PCKRTN result = pck_open(filepath);
    wprintf(L"pck_open returned: %d\n", result);

    if (result == WINPCK_OK) {
        wprintf(L"File opened successfully!\n");

        if (pck_IsValidPck()) {
            wprintf(L"Valid PCK file\n");
            wprintf(L"File count: %u\n", pck_filecount());
            wprintf(L"File size: %llu\n", pck_filesize());
        } else {
            wprintf(L"Invalid PCK file\n");
        }

        pck_close();
    } else {
        wprintf(L"Failed to open PCK file\n");
    }

    return 0;
}
