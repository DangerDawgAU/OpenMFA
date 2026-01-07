#include "common.h"

// Forward declaration
class CSmartCardCredential;

class CSmartCardProvider : public ICredentialProvider
{
public:
    CSmartCardProvider();
    ~CSmartCardProvider();

    // IUnknown
    IFACEMETHODIMP_(ULONG) AddRef() { return InterlockedIncrement(&_cRef); }
    IFACEMETHODIMP_(ULONG) Release()
    {
        LONG cRef = InterlockedDecrement(&_cRef);
        if (!cRef)
        {
            delete this;
            InterlockedDecrement(&g_cRef);
        }
        return cRef;
    }
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv);

    // ICredentialProvider
    IFACEMETHODIMP SetUsageScenario(CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, DWORD dwFlags);
    IFACEMETHODIMP SetSerialization(const CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs);
    IFACEMETHODIMP Advise(ICredentialProviderEvents* pcpe, UINT_PTR upAdviseContext);
    IFACEMETHODIMP UnAdvise();
    IFACEMETHODIMP GetFieldDescriptorCount(DWORD* pdwCount);
    IFACEMETHODIMP GetFieldDescriptorAt(DWORD dwIndex, CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** ppcpfd);
    IFACEMETHODIMP GetCredentialCount(DWORD* pdwCount, DWORD* pdwDefault, BOOL* pbAutoLogonWithDefault);
    IFACEMETHODIMP GetCredentialAt(DWORD dwIndex, ICredentialProviderCredential** ppcpc);

private:
    HRESULT _DetectSmartCard();
    HRESULT _ReadUsername();
    void _CleanupCredential();

    long _cRef;
    CREDENTIAL_PROVIDER_USAGE_SCENARIO _cpus;
    CSmartCardCredential* _pCredential;
    WCHAR _wszUsername[256];
    BOOL _bSmartCardPresent;
};

// Field descriptors
static const FIELD_DESCRIPTOR s_rgFieldDescriptors[] =
{
    { CPFT_LARGE_TEXT, L"Username" },          // FID_USERNAME
    { CPFT_PASSWORD_TEXT, L"PIN" },            // FID_PIN
    { CPFT_SUBMIT_BUTTON, L"Sign in" },        // FID_SUBMIT
};

// Constructor
CSmartCardProvider::CSmartCardProvider() :
    _cRef(1),
    _cpus(CPUS_INVALID),
    _pCredential(NULL),
    _bSmartCardPresent(FALSE)
{
    InterlockedIncrement(&g_cRef);
    _wszUsername[0] = L'\0';
}

// Destructor
CSmartCardProvider::~CSmartCardProvider()
{
    _CleanupCredential();
}

// QueryInterface
HRESULT CSmartCardProvider::QueryInterface(REFIID riid, void** ppv)
{
    static const QITAB qit[] = {
        QITABENT(CSmartCardProvider, ICredentialProvider),
        {0},
    };
    return QISearch(this, qit, riid, ppv);
}

// SetUsageScenario - called when credential provider is activated
HRESULT CSmartCardProvider::SetUsageScenario(
    CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus,
    DWORD dwFlags)
{
    _cpus = cpus;

    // Only support unlock and logon scenarios
    if (cpus == CPUS_UNLOCK_WORKSTATION || cpus == CPUS_LOGON)
    {
        // Detect smart card
        return _DetectSmartCard();
    }

    return E_NOTIMPL;
}

// SetSerialization - not used for smart cards
HRESULT CSmartCardProvider::SetSerialization(
    const CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs)
{
    return E_NOTIMPL;
}

// Advise - not implementing event notifications for MVP
HRESULT CSmartCardProvider::Advise(ICredentialProviderEvents* pcpe, UINT_PTR upAdviseContext)
{
    return E_NOTIMPL;
}

HRESULT CSmartCardProvider::UnAdvise()
{
    return E_NOTIMPL;
}

// GetFieldDescriptorCount
HRESULT CSmartCardProvider::GetFieldDescriptorCount(DWORD* pdwCount)
{
    *pdwCount = FID_COUNT;
    return S_OK;
}

// GetFieldDescriptorAt
HRESULT CSmartCardProvider::GetFieldDescriptorAt(
    DWORD dwIndex,
    CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** ppcpfd)
{
    if (dwIndex >= FID_COUNT)
        return E_INVALIDARG;

    CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR* pcpfd =
        (CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR*)CoTaskMemAlloc(sizeof(CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR));

    if (!pcpfd)
        return E_OUTOFMEMORY;

    pcpfd->dwFieldID = dwIndex;
    pcpfd->cpft = s_rgFieldDescriptors[dwIndex].cpft;

    HRESULT hr = SHStrDupW(s_rgFieldDescriptors[dwIndex].pszLabel, &pcpfd->pszLabel);

    if (SUCCEEDED(hr))
    {
        *ppcpfd = pcpfd;
    }
    else
    {
        CoTaskMemFree(pcpfd);
    }

    return hr;
}

// GetCredentialCount
HRESULT CSmartCardProvider::GetCredentialCount(
    DWORD* pdwCount,
    DWORD* pdwDefault,
    BOOL* pbAutoLogonWithDefault)
{
    *pdwDefault = CREDENTIAL_PROVIDER_NO_DEFAULT;
    *pbAutoLogonWithDefault = FALSE;

    if (_bSmartCardPresent && _pCredential)
    {
        *pdwCount = 1;
    }
    else
    {
        *pdwCount = 0;
    }

    return S_OK;
}

// GetCredentialAt
HRESULT CSmartCardProvider::GetCredentialAt(
    DWORD dwIndex,
    ICredentialProviderCredential** ppcpc)
{
    if (dwIndex != 0 || !_pCredential)
        return E_INVALIDARG;

    return _pCredential->QueryInterface(IID_PPV_ARGS(ppcpc));
}

// Detect smart card and read username
HRESULT CSmartCardProvider::_DetectSmartCard()
{
    _bSmartCardPresent = FALSE;
    _wszUsername[0] = L'\0';

    // Open smart card context
    SCARDCONTEXT hContext;
    LONG lResult = SCardEstablishContext(SCARD_SCOPE_USER, NULL, NULL, &hContext);

    if (lResult != SCARD_S_SUCCESS)
        return E_FAIL;

    // List readers
    DWORD dwReaders = SCARD_AUTOALLOCATE;
    LPWSTR pmszReaders = NULL;

    lResult = SCardListReadersW(hContext, NULL, (LPWSTR)&pmszReaders, &dwReaders);

    if (lResult == SCARD_S_SUCCESS && pmszReaders)
    {
        // Use first reader (MVP simplification)
        SCARDHANDLE hCard;
        DWORD dwActiveProtocol;

        lResult = SCardConnectW(hContext, pmszReaders, SCARD_SHARE_SHARED,
            SCARD_PROTOCOL_T0 | SCARD_PROTOCOL_T1, &hCard, &dwActiveProtocol);

        if (lResult == SCARD_S_SUCCESS)
        {
            _bSmartCardPresent = TRUE;

            // Read username from certificate
            _ReadUsername();

            // Create credential object
            _CleanupCredential();
            _pCredential = new (std::nothrow) CSmartCardCredential();
            if (_pCredential)
            {
                _pCredential->Initialize(_wszUsername);
            }

            SCardDisconnect(hCard, SCARD_LEAVE_CARD);
        }

        SCardFreeMemory(hContext, pmszReaders);
    }

    SCardReleaseContext(hContext);

    return _bSmartCardPresent ? S_OK : E_FAIL;
}

// Read username from certificate
HRESULT CSmartCardProvider::_ReadUsername()
{
    // Simplified: Try to open certificate store for smart card
    HCERTSTORE hStore = CertOpenStore(
        CERT_STORE_PROV_SYSTEM,
        0,
        NULL,
        CERT_SYSTEM_STORE_CURRENT_USER,
        L"MY");

    if (!hStore)
        return E_FAIL;

    // Find certificate with private key on smart card
    PCCERT_CONTEXT pCertContext = NULL;
    while ((pCertContext = CertEnumCertificatesInStore(hStore, pCertContext)) != NULL)
    {
        // Check if certificate has smart card key
        DWORD dwKeySpec;
        BOOL bCallerFree;
        HCRYPTPROV_OR_NCRYPT_KEY_HANDLE hKey;

        if (CryptAcquireCertificatePrivateKey(pCertContext, CRYPT_ACQUIRE_CACHE_FLAG,
            NULL, &hKey, &dwKeySpec, &bCallerFree))
        {
            // Extract Common Name from subject
            DWORD dwSize = CertGetNameStringW(pCertContext, CERT_NAME_SIMPLE_DISPLAY_TYPE,
                0, NULL, NULL, 0);

            if (dwSize > 0 && dwSize <= ARRAYSIZE(_wszUsername))
            {
                CertGetNameStringW(pCertContext, CERT_NAME_SIMPLE_DISPLAY_TYPE,
                    0, NULL, _wszUsername, dwSize);
            }

            if (bCallerFree)
            {
                if (dwKeySpec == CERT_NCRYPT_KEY_SPEC)
                    NCryptFreeObject(hKey);
                else
                    CryptReleaseContext(hKey, 0);
            }

            break; // Use first certificate found
        }
    }

    if (pCertContext)
        CertFreeCertificateContext(pCertContext);

    CertCloseStore(hStore, 0);

    return (_wszUsername[0] != L'\0') ? S_OK : E_FAIL;
}

void CSmartCardProvider::_CleanupCredential()
{
    if (_pCredential)
    {
        _pCredential->Release();
        _pCredential = NULL;
    }
}

// Factory function
HRESULT SmartCardProvider_CreateInstance(REFIID riid, void** ppv)
{
    CSmartCardProvider* pProvider = new (std::nothrow) CSmartCardProvider();
    if (!pProvider)
        return E_OUTOFMEMORY;

    HRESULT hr = pProvider->QueryInterface(riid, ppv);
    pProvider->Release();
    return hr;
}
