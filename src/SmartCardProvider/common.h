#pragma once

// Windows headers
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <unknwn.h>
#include <credentialprovider.h>
#include <wincrypt.h>
#include <winscard.h>
#include <ntsecapi.h>
#include <shlwapi.h>

// C++ headers
#include <strsafe.h>

// Provider GUID: {8FF56996-6BED-4EA2-A465-1486EDCE3A92}
DEFINE_GUID(CLSID_SmartCardProvider,
    0x8FF56996, 0x6BED, 0x4EA2, 0xA4, 0x65, 0x14, 0x86, 0xED, 0xCE, 0x3A, 0x92);

// Field IDs for our credential tile
enum FIELD_ID {
    FID_USERNAME = 0,    // Read-only username from certificate
    FID_PIN = 1,         // Password field for PIN entry
    FID_SUBMIT = 2,      // Submit button
    FID_COUNT = 3        // Total field count
};

// Field descriptors
struct FIELD_DESCRIPTOR {
    CREDENTIAL_PROVIDER_FIELD_TYPE cpft;
    LPCWSTR pszLabel;
};

// Global variables
extern HINSTANCE g_hInst;   // DLL instance handle
extern long g_cRef;          // COM reference count

// Helper macros
#define RELEASE_IF_NOT_NULL(p) if (p) { (p)->Release(); (p) = NULL; }
#define FREE_IF_NOT_NULL(p) if (p) { CoTaskMemFree(p); (p) = NULL; }
