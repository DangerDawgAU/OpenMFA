#include "common.h"

class CSmartCardCredential : public ICredentialProviderCredential
{
public:
    CSmartCardCredential();
    ~CSmartCardCredential();

    // IUnknown
    IFACEMETHODIMP_(ULONG) AddRef() { return InterlockedIncrement(&_cRef); }
    IFACEMETHODIMP_(ULONG) Release()
    {
        LONG cRef = InterlockedDecrement(&_cRef);
        if (!cRef) delete this;
        return cRef;
    }
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv);

    // ICredentialProviderCredential
    IFACEMETHODIMP Advise(ICredentialProviderCredentialEvents* pcpce);
    IFACEMETHODIMP UnAdvise();
    IFACEMETHODIMP SetSelected(BOOL* pbAutoLogon);
    IFACEMETHODIMP SetDeselected();
    IFACEMETHODIMP GetFieldState(DWORD dwFieldID, CREDENTIAL_PROVIDER_FIELD_STATE* pcpfs,
        CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE* pcpfis);
    IFACEMETHODIMP GetStringValue(DWORD dwFieldID, LPWSTR* ppwsz);
    IFACEMETHODIMP GetBitmapValue(DWORD dwFieldID, HBITMAP* phbmp);
    IFACEMETHODIMP GetCheckboxValue(DWORD dwFieldID, BOOL* pbChecked, LPWSTR* ppwszLabel);
    IFACEMETHODIMP GetComboBoxValueCount(DWORD dwFieldID, DWORD* pcItems, DWORD* pdwSelectedItem);
    IFACEMETHODIMP GetComboBoxValueAt(DWORD dwFieldID, DWORD dwItem, LPWSTR* ppwszItem);
    IFACEMETHODIMP GetSubmitButtonValue(DWORD dwFieldID, DWORD* pdwAdjacentTo);
    IFACEMETHODIMP SetStringValue(DWORD dwFieldID, LPCWSTR pwz);
    IFACEMETHODIMP SetCheckboxValue(DWORD dwFieldID, BOOL bChecked);
    IFACEMETHODIMP SetComboBoxSelectedValue(DWORD dwFieldID, DWORD dwSelectedItem);
    IFACEMETHODIMP CommandLinkClicked(DWORD dwFieldID);
    IFACEMETHODIMP GetSerialization(CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE* pcpgsr,
        CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs, LPWSTR* ppwszOptionalStatusText,
        CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon);
    IFACEMETHODIMP ReportResult(NTSTATUS ntsStatus, NTSTATUS ntsSubstatus,
        LPWSTR* ppwszOptionalStatusText, CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon);

    // Custom methods
    HRESULT Initialize(LPCWSTR pwszUsername);

private:
    HRESULT _ValidatePIN();

    long _cRef;
    WCHAR _wszUsername[256];
    WCHAR _wszPin[32];
};

// Constructor
CSmartCardCredential::CSmartCardCredential() : _cRef(1)
{
    _wszUsername[0] = L'\0';
    _wszPin[0] = L'\0';
}

// Destructor
CSmartCardCredential::~CSmartCardCredential()
{
    // Clear PIN from memory
    SecureZeroMemory(_wszPin, sizeof(_wszPin));
}

// QueryInterface
HRESULT CSmartCardCredential::QueryInterface(REFIID riid, void** ppv)
{
    static const QITAB qit[] = {
        QITABENT(CSmartCardCredential, ICredentialProviderCredential),
        {0},
    };
    return QISearch(this, qit, riid, ppv);
}

// Initialize with username
HRESULT CSmartCardCredential::Initialize(LPCWSTR pwszUsername)
{
    return StringCchCopyW(_wszUsername, ARRAYSIZE(_wszUsername), pwszUsername);
}

// Advise - not implementing events for MVP
HRESULT CSmartCardCredential::Advise(ICredentialProviderCredentialEvents* pcpce)
{
    return E_NOTIMPL;
}

HRESULT CSmartCardCredential::UnAdvise()
{
    return E_NOTIMPL;
}

// SetSelected
HRESULT CSmartCardCredential::SetSelected(BOOL* pbAutoLogon)
{
    *pbAutoLogon = FALSE;
    return S_OK;
}

HRESULT CSmartCardCredential::SetDeselected()
{
    return S_OK;
}

// GetFieldState - controls field visibility and interaction
HRESULT CSmartCardCredential::GetFieldState(
    DWORD dwFieldID,
    CREDENTIAL_PROVIDER_FIELD_STATE* pcpfs,
    CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE* pcpfis)
{
    if (dwFieldID >= FID_COUNT)
        return E_INVALIDARG;

    switch (dwFieldID)
    {
    case FID_USERNAME:
        *pcpfs = CPFS_DISPLAY_IN_SELECTED_TILE;
        *pcpfis = CPFIS_READONLY;
        break;

    case FID_PIN:
        *pcpfs = CPFS_DISPLAY_IN_SELECTED_TILE;
        *pcpfis = CPFIS_FOCUSED;
        break;

    case FID_SUBMIT:
        *pcpfs = CPFS_DISPLAY_IN_SELECTED_TILE;
        *pcpfis = CPFIS_NONE;
        break;
    }

    return S_OK;
}

// GetStringValue - returns field values
HRESULT CSmartCardCredential::GetStringValue(DWORD dwFieldID, LPWSTR* ppwsz)
{
    HRESULT hr = E_INVALIDARG;

    if (dwFieldID == FID_USERNAME)
    {
        hr = SHStrDupW(_wszUsername, ppwsz);
    }
    else if (dwFieldID == FID_PIN)
    {
        // Return empty string for password field
        hr = SHStrDupW(L"", ppwsz);
    }

    return hr;
}

// GetBitmapValue - not using custom icons for MVP
HRESULT CSmartCardCredential::GetBitmapValue(DWORD dwFieldID, HBITMAP* phbmp)
{
    return E_NOTIMPL;
}

HRESULT CSmartCardCredential::GetCheckboxValue(DWORD dwFieldID, BOOL* pbChecked, LPWSTR* ppwszLabel)
{
    return E_NOTIMPL;
}

HRESULT CSmartCardCredential::GetComboBoxValueCount(DWORD dwFieldID, DWORD* pcItems, DWORD* pdwSelectedItem)
{
    return E_NOTIMPL;
}

HRESULT CSmartCardCredential::GetComboBoxValueAt(DWORD dwFieldID, DWORD dwItem, LPWSTR* ppwszItem)
{
    return E_NOTIMPL;
}

// GetSubmitButtonValue - PIN field submits when Enter pressed
HRESULT CSmartCardCredential::GetSubmitButtonValue(DWORD dwFieldID, DWORD* pdwAdjacentTo)
{
    if (dwFieldID == FID_SUBMIT)
    {
        *pdwAdjacentTo = FID_PIN;
        return S_OK;
    }
    return E_INVALIDARG;
}

// SetStringValue - called when user enters PIN
HRESULT CSmartCardCredential::SetStringValue(DWORD dwFieldID, LPCWSTR pwz)
{
    if (dwFieldID == FID_PIN)
    {
        return StringCchCopyW(_wszPin, ARRAYSIZE(_wszPin), pwz);
    }
    return E_INVALIDARG;
}

HRESULT CSmartCardCredential::SetCheckboxValue(DWORD dwFieldID, BOOL bChecked)
{
    return E_NOTIMPL;
}

HRESULT CSmartCardCredential::SetComboBoxSelectedValue(DWORD dwFieldID, DWORD dwSelectedItem)
{
    return E_NOTIMPL;
}

HRESULT CSmartCardCredential::CommandLinkClicked(DWORD dwFieldID)
{
    return E_NOTIMPL;
}

// GetSerialization - called when user clicks submit
// This is where we validate PIN and create auth package
HRESULT CSmartCardCredential::GetSerialization(
    CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE* pcpgsr,
    CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs,
    LPWSTR* ppwszOptionalStatusText,
    CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon)
{
    *pcpgsr = CPGSR_NO_CREDENTIAL_NOT_FINISHED;
    *ppwszOptionalStatusText = NULL;
    *pcpsiOptionalStatusIcon = CPSI_NONE;

    // Validate PIN with smart card
    HRESULT hr = _ValidatePIN();

    if (FAILED(hr))
    {
        // PIN validation failed
        SHStrDupW(L"Incorrect PIN or smart card error", ppwszOptionalStatusText);
        *pcpsiOptionalStatusIcon = CPSI_ERROR;
        return S_OK;
    }

    // PIN valid - create Kerberos auth package
    KERB_INTERACTIVE_UNLOCK_LOGON kiul;
    ZeroMemory(&kiul, sizeof(kiul));

    kiul.Logon.MessageType = KerbInteractiveLogon;

    // Username
    USHORT usernameLen = (USHORT)wcslen(_wszUsername) * sizeof(WCHAR);
    kiul.Logon.UserName.Length = usernameLen;
    kiul.Logon.UserName.MaximumLength = usernameLen;

    // Domain (empty for local accounts)
    kiul.Logon.LogonDomainName.Length = 0;
    kiul.Logon.LogonDomainName.MaximumLength = 0;

    // Password (use PIN)
    USHORT pinLen = (USHORT)wcslen(_wszPin) * sizeof(WCHAR);
    kiul.Logon.Password.Length = pinLen;
    kiul.Logon.Password.MaximumLength = pinLen;

    // Calculate total size needed
    size_t cbTotal = sizeof(kiul) + usernameLen + pinLen;

    BYTE* rgbSerialization = (BYTE*)CoTaskMemAlloc(cbTotal);
    if (!rgbSerialization)
        return E_OUTOFMEMORY;

    // Copy structure
    KERB_INTERACTIVE_UNLOCK_LOGON* pkiul = (KERB_INTERACTIVE_UNLOCK_LOGON*)rgbSerialization;
    *pkiul = kiul;

    // Copy username
    BYTE* pbBuffer = rgbSerialization + sizeof(kiul);
    memcpy(pbBuffer, _wszUsername, usernameLen);
    pkiul->Logon.UserName.Buffer = (PWSTR)(pbBuffer - rgbSerialization);
    pbBuffer += usernameLen;

    // Copy PIN
    memcpy(pbBuffer, _wszPin, pinLen);
    pkiul->Logon.Password.Buffer = (PWSTR)(pbBuffer - rgbSerialization);

    // Get Kerberos package ID
    ULONG ulAuthPackage;
    LSA_STRING lsaAuthString;
    lsaAuthString.Buffer = (PCHAR)NEGOSSP_NAME_A;
    lsaAuthString.Length = (USHORT)strlen(lsaAuthString.Buffer);
    lsaAuthString.MaximumLength = lsaAuthString.Length;

    HANDLE hLsa;
    NTSTATUS status = LsaConnectUntrusted(&hLsa);
    if (status == STATUS_SUCCESS)
    {
        status = LsaLookupAuthenticationPackage(hLsa, &lsaAuthString, &ulAuthPackage);
        LsaDeregisterLogonProcess(hLsa);
    }

    if (status != STATUS_SUCCESS)
    {
        CoTaskMemFree(rgbSerialization);
        return HRESULT_FROM_NT(status);
    }

    // Fill serialization structure
    pcpcs->ulAuthenticationPackage = ulAuthPackage;
    pcpcs->cbSerialization = (DWORD)cbTotal;
    pcpcs->rgbSerialization = rgbSerialization;
    pcpcs->clsidCredentialProvider = CLSID_SmartCardProvider;

    *pcpgsr = CPGSR_RETURN_CREDENTIAL_FINISHED;

    return S_OK;
}

// ReportResult - called after authentication attempt
HRESULT CSmartCardCredential::ReportResult(
    NTSTATUS ntsStatus,
    NTSTATUS ntsSubstatus,
    LPWSTR* ppwszOptionalStatusText,
    CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon)
{
    *ppwszOptionalStatusText = NULL;
    *pcpsiOptionalStatusIcon = CPSI_NONE;

    // Clear PIN
    SecureZeroMemory(_wszPin, sizeof(_wszPin));

    return S_OK;
}

// Validate PIN with smart card
HRESULT CSmartCardCredential::_ValidatePIN()
{
    // Open certificate store
    HCERTSTORE hStore = CertOpenStore(CERT_STORE_PROV_SYSTEM, 0, NULL,
        CERT_SYSTEM_STORE_CURRENT_USER, L"MY");

    if (!hStore)
        return E_FAIL;

    // Find certificate for this username
    PCCERT_CONTEXT pCertContext = CertFindCertificateInStore(hStore,
        X509_ASN_ENCODING | PKCS_7_ASN_ENCODING, 0, CERT_FIND_SUBJECT_STR,
        _wszUsername, NULL);

    if (!pCertContext)
    {
        CertCloseStore(hStore, 0);
        return E_FAIL;
    }

    // Try to acquire private key with PIN
    DWORD dwKeySpec;
    BOOL bCallerFree = FALSE;
    HCRYPTPROV_OR_NCRYPT_KEY_HANDLE hKey;

    HRESULT hr = E_FAIL;

    // Acquire key with silent flag - will fail if PIN incorrect
    if (CryptAcquireCertificatePrivateKey(pCertContext,
        CRYPT_ACQUIRE_SILENT_FLAG | CRYPT_ACQUIRE_COMPARE_KEY_FLAG,
        NULL, &hKey, &dwKeySpec, &bCallerFree))
    {
        hr = S_OK;

        if (bCallerFree)
        {
            if (dwKeySpec == CERT_NCRYPT_KEY_SPEC)
                NCryptFreeObject(hKey);
            else
                CryptReleaseContext(hKey, 0);
        }
    }

    CertFreeCertificateContext(pCertContext);
    CertCloseStore(hStore, 0);

    return hr;
}
