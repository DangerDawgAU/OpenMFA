#include "common.h"

// Forward declarations
HRESULT SmartCardProvider_CreateInstance(REFIID riid, void** ppv);

// Global variables
HINSTANCE g_hInst = NULL;
long g_cRef = 0;

// DLL Entry Point
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        g_hInst = hModule;
        DisableThreadLibraryCalls(hModule);
        break;
    }
    return TRUE;
}

// COM Class Factory
class CClassFactory : public IClassFactory
{
public:
    CClassFactory() : _cRef(1) {}

    // IUnknown
    IFACEMETHODIMP_(ULONG) AddRef() { return InterlockedIncrement(&_cRef); }
    IFACEMETHODIMP_(ULONG) Release()
    {
        LONG cRef = InterlockedDecrement(&_cRef);
        if (!cRef) delete this;
        return cRef;
    }
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv)
    {
        static const QITAB qit[] = {
            QITABENT(CClassFactory, IClassFactory),
            {0},
        };
        return QISearch(this, qit, riid, ppv);
    }

    // IClassFactory
    IFACEMETHODIMP CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv)
    {
        *ppv = NULL;
        if (pUnkOuter != NULL)
            return CLASS_E_NOAGGREGATION;

        return SmartCardProvider_CreateInstance(riid, ppv);
    }

    IFACEMETHODIMP LockServer(BOOL bLock)
    {
        if (bLock)
            InterlockedIncrement(&g_cRef);
        else
            InterlockedDecrement(&g_cRef);
        return S_OK;
    }

private:
    long _cRef;
};

// DLL Exports
STDAPI DllCanUnloadNow()
{
    return (g_cRef > 0) ? S_FALSE : S_OK;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void** ppv)
{
    *ppv = NULL;

    if (rclsid != CLSID_SmartCardProvider)
        return CLASS_E_CLASSNOTAVAILABLE;

    CClassFactory* pcf = new (std::nothrow) CClassFactory();
    if (!pcf)
        return E_OUTOFMEMORY;

    HRESULT hr = pcf->QueryInterface(riid, ppv);
    pcf->Release();
    return hr;
}

// Registry registration
STDAPI DllRegisterServer()
{
    HRESULT hr;
    wchar_t szCLSID[40];
    wchar_t szSubkey[MAX_PATH];
    wchar_t szModulePath[MAX_PATH];

    // Get DLL path
    GetModuleFileNameW(g_hInst, szModulePath, ARRAYSIZE(szModulePath));

    // Convert CLSID to string
    StringFromGUID2(CLSID_SmartCardProvider, szCLSID, ARRAYSIZE(szCLSID));

    // Register CLSID
    StringCchPrintfW(szSubkey, ARRAYSIZE(szSubkey),
        L"CLSID\\%s", szCLSID);

    HKEY hKey;
    LONG lResult = RegCreateKeyExW(HKEY_CLASSES_ROOT, szSubkey, 0, NULL,
        REG_OPTION_NON_VOLATILE, KEY_WRITE, NULL, &hKey, NULL);

    if (lResult != ERROR_SUCCESS)
        return HRESULT_FROM_WIN32(lResult);

    RegSetValueExW(hKey, NULL, 0, REG_SZ,
        (BYTE*)L"SmartCardProvider", sizeof(L"SmartCardProvider"));
    RegCloseKey(hKey);

    // Register InprocServer32
    StringCchPrintfW(szSubkey, ARRAYSIZE(szSubkey),
        L"CLSID\\%s\\InprocServer32", szCLSID);

    lResult = RegCreateKeyExW(HKEY_CLASSES_ROOT, szSubkey, 0, NULL,
        REG_OPTION_NON_VOLATILE, KEY_WRITE, NULL, &hKey, NULL);

    if (lResult != ERROR_SUCCESS)
        return HRESULT_FROM_WIN32(lResult);

    RegSetValueExW(hKey, NULL, 0, REG_SZ,
        (BYTE*)szModulePath, (DWORD)((wcslen(szModulePath) + 1) * sizeof(wchar_t)));
    RegSetValueExW(hKey, L"ThreadingModel", 0, REG_SZ,
        (BYTE*)L"Apartment", sizeof(L"Apartment"));
    RegCloseKey(hKey);

    // Register as Credential Provider
    StringCchPrintfW(szSubkey, ARRAYSIZE(szSubkey),
        L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\Credential Providers\\%s",
        szCLSID);

    lResult = RegCreateKeyExW(HKEY_LOCAL_MACHINE, szSubkey, 0, NULL,
        REG_OPTION_NON_VOLATILE, KEY_WRITE, NULL, &hKey, NULL);

    if (lResult != ERROR_SUCCESS)
        return HRESULT_FROM_WIN32(lResult);

    RegSetValueExW(hKey, NULL, 0, REG_SZ,
        (BYTE*)L"SmartCardProvider", sizeof(L"SmartCardProvider"));
    RegCloseKey(hKey);

    return S_OK;
}

STDAPI DllUnregisterServer()
{
    wchar_t szCLSID[40];
    wchar_t szSubkey[MAX_PATH];

    StringFromGUID2(CLSID_SmartCardProvider, szCLSID, ARRAYSIZE(szCLSID));

    // Unregister from Credential Providers
    StringCchPrintfW(szSubkey, ARRAYSIZE(szSubkey),
        L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\Credential Providers\\%s",
        szCLSID);
    RegDeleteTreeW(HKEY_LOCAL_MACHINE, szSubkey);

    // Unregister CLSID
    StringCchPrintfW(szSubkey, ARRAYSIZE(szSubkey), L"CLSID\\%s", szCLSID);
    RegDeleteTreeW(HKEY_CLASSES_ROOT, szSubkey);

    return S_OK;
}
