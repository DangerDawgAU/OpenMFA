SmartCardProvider - Windows Credential Provider for Smart Card Authentication
==============================================================================

OVERVIEW
--------
This is a minimal viable Windows Credential Provider that enables local account
login using smart card + PIN authentication. It integrates with the Windows
login screen to provide passwordless authentication.

CLSID: {8FF56996-6BED-4EA2-A465-1486EDCE3A92}

PREREQUISITES
-------------
1. Visual Studio 2022 with "Desktop development with C++" workload
2. Windows SDK 10.0 or newer
3. Smart card reader
4. MyEID card configured with:
   - Certificate installed on card
   - Certificate Common Name (CN) matching a local Windows username
   - Certificate in Windows certificate store (Current User -> Personal)
5. Local Windows user account with password

BUILD INSTRUCTIONS
------------------
1. Open SmartCardProvider.sln in Visual Studio 2022
2. Select "Release" configuration and "x64" platform
3. Build -> Build Solution (F7)
4. Output: x64\Release\SmartCardProvider.dll

INSTALLATION
------------
1. Copy SmartCardProvider.dll to a safe location (e.g., C:\SmartCard\)
2. Copy register.bat to the same location
3. Right-click register.bat and select "Run as administrator"
4. Verify "SUCCESS!" message appears

TESTING
-------
1. Insert smart card into reader
2. Press Windows+L to lock the workstation
3. Smart card credential tile should appear with username from certificate
4. Enter PIN in the PIN field
5. Click "Sign in" or press Enter
6. Should authenticate and unlock the workstation

TROUBLESHOOTING
---------------
Provider doesn't appear:
- Check Event Viewer -> Windows Logs -> System for errors
- Verify smart card is inserted and detected: certutil -scinfo
- Check registry key exists:
  HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{8FF56996-6BED-4EA2-A465-1486EDCE3A92}

Authentication fails:
- Verify certificate CN exactly matches local username
- Test PIN separately: certutil -scinfo
- Check certificate is in Personal store: certmgr.msc
- Ensure local account has a password set

UNINSTALLATION
--------------
1. Right-click unregister.bat and select "Run as administrator"
2. Restart computer (recommended)

ARCHITECTURE
------------
Files:
- common.h              - Shared definitions and GUID
- DllMain.cpp           - COM DLL infrastructure
- SmartCardProvider.cpp - ICredentialProvider implementation
- SmartCardCredential.cpp - ICredentialProviderCredential implementation
- SmartCardProvider.def - DLL exports

Flow:
1. Windows detects smart card -> calls SetUsageScenario()
2. Provider detects card -> reads certificate -> extracts username
3. Provider creates credential tile
4. User enters PIN
5. Provider validates PIN with smart card
6. Provider creates authentication package for Windows LSA
7. Windows authenticates user

SECURITY NOTES
--------------
- PINs are cleared from memory using SecureZeroMemory()
- Private keys never leave the smart card
- Uses Windows smart card crypto provider for PIN validation
- Supports only local accounts (no domain authentication)

LIMITATIONS (MVP)
-----------------
- Single smart card reader support
- Uses first certificate found on card
- Local accounts only (no Active Directory)
- No background card monitoring
- No custom UI icons

For detailed implementation information, see:
AI Support\CREDENTIAL_PROVIDER_IMPLEMENTATION_GUIDE.md

SUPPORT
-------
This is a minimal viable implementation for proof of concept.
For production use, additional hardening and features are recommended.
