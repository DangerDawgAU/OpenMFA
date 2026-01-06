# Smart Card Logon on Windows Workgroup/Standalone Computers

## Problem Discovery

After extensive testing and research, we discovered that **Windows does NOT support native smart card logon for local/workgroup accounts**. The built-in Windows smart card logon requires:
- Windows Active Directory domain
- Kerberos authentication
- Domain controller

This explains why our properly configured certificates (with Smart Card Logon EKU and UPN) showed "no valid certificates found" - the feature simply isn't available in workgroup mode.

**Sources:**
- [Smart card logon on Windows without any domain](https://social.msdn.microsoft.com/Forums/windowsdesktop/en-US/be47fb0b-e046-4900-bcca-b9aeb80fa2fb/smart-card-logon-on-windows-without-any-domain-for-local-user-accounts)
- [Is a Windows Domain required for Windows smart card logon?](https://pivkey.zendesk.com/hc/en-us/articles/203128519-Is-a-Windows-Domain-required-for-Windows-smart-card-logon)

## Solution: EIDAuthenticate

**EIDAuthenticate** by MySmartLogon is a third-party Windows Credential Provider that enables smart card logon on standalone/workgroup computers.

### Product Information

**Website:** https://www.mysmartlogon.com/products/eidauthenticate.html

**Key Features:**
- Enables smart card logon on standalone Windows computers
- Works with local Windows accounts (not just domain accounts)
- Authenticates inside Windows security kernel (lsass.exe)
- Supports cards with CSP or minidriver
- Works with OpenSC-supported cards

**Editions:**
1. **Community Edition** - Free, but restricted to Windows Home editions only
2. **Enterprise Edition** - For Windows Pro/Enterprise, commercial license required

### How It Works

EIDAuthenticate acts as a Windows Credential Provider that:
1. Intercepts the Windows logon screen
2. Reads certificates from smart cards
3. Maps certificates to local Windows user accounts
4. Performs authentication without requiring Active Directory

**Authentication:** Happens inside Windows security kernel (lsass.exe), not in user space.

### Setup Process (Based on Documentation)

1. **Install card vendor drivers/tools first** (e.g., OpenSC for MyEID cards)
2. **Initialize smart card** with vendor tools
3. **Install EIDAuthenticate**
4. **Run EIDAuthenticate Configuration Wizard** from Windows Control Panel
   - Wizard creates self-signed root certificate
   - Generates certificate for the user
   - Installs certificate to card
   - Maps certificate to Windows local user account
5. **Test authentication** with the PIN test in the wizard

### Certificate Requirements

From the documentation:
- Certificate can be **self-signed** (wizard creates one)
- Certificate must be on the smart card
- Root certificate added to Windows Trusted Root Certification Authorities
- Example from guide: Certificate for user 'Lauri', issued by 'Lauri-PC'

**Note:** The wizard handles certificate creation and mapping automatically.

### OpenSC Card Support

From setup guide:
- EIDAuthenticate requires "A Smart Card coming with CSP support or with a mini driver"
- Works with OpenSC cards via OpenSC minidriver
- Can use OpenSC tools (`pkcs15-tool`, OpenSSL) to inspect card after setup
- MySmartLogon also provides separate "OpenPGP CSP and minidriver" for OpenPGP cards

### Verification Tools

After setup:
- `certutil -scinfo` - Verify Windows recognizes the card and certificate
- `certmgr.msc` - Certificate Manager to view installed certificates
- `EIDLogManager` - Troubleshooting tool included with EIDAuthenticate

**Sources:**
- [EIDAuthenticate Product Page](https://www.mysmartlogon.com/products/eidauthenticate.html)
- [EIDAuthenticate Setup Guide (archived)](https://saisa.eu/blogs/Guidance/?p=887)
- [Setting up EIDAuthenticate with OpenPGP card](https://github.com/djozsef/openpgp-docs/blob/master/Setting%20up%20EIDAuthenticate%20with%20OpenPGP%20card.md)

## Implications for OpenMFA

### Current State

Our OpenMFA application successfully:
- ✅ Initializes MyEID cards with PKCS#15 structure
- ✅ Generates RSA keys on the card
- ✅ Creates CSRs with proper UPN and Smart Card Logon EKU
- ✅ Signs certificates with local CA
- ✅ Imports certificates to card and Windows store
- ✅ All certificates have correct attributes for smart card logon

However, **native Windows smart card logon won't work without Active Directory**.

### Options for OpenMFA

#### Option 1: Target Domain-Joined Computers Only
Change scope to explicitly target Windows computers joined to Active Directory domains. This is the "official" Windows smart card logon scenario.

**Pros:**
- Uses native Windows features
- No third-party dependencies
- Our current implementation would work perfectly
- Standard enterprise scenario

**Cons:**
- Excludes home users and small businesses without AD
- Requires existing domain infrastructure

#### Option 2: Integrate with EIDAuthenticate
Partner with or integrate EIDAuthenticate for standalone computer support.

**Pros:**
- Enables workgroup/standalone computer support
- Works with our existing card initialization
- Proven solution that works with OpenSC

**Cons:**
- Requires users to install third-party software
- Community Edition limited to Windows Home
- Enterprise Edition requires commercial license
- Can't bundle directly without licensing agreement

#### Option 3: Develop Custom Credential Provider
Create our own Windows Credential Provider (like EIDAuthenticate).

**Pros:**
- Full control over the solution
- Can bundle with OpenMFA
- No third-party licensing issues

**Cons:**
- Significant development effort
- Credential Provider API is complex
- Security implications (runs in lsass.exe)
- Would need extensive testing
- Requires code signing certificate

#### Option 4: Alternative Use Cases
Pivot to non-Windows-logon smart card use cases:

- SSH authentication with smart cards
- Application-level authentication (browsers, VPNs)
- Email signing/encryption
- Code signing
- Document signing

**Pros:**
- Works on any Windows edition
- No domain or credential provider needed
- Our current implementation supports these scenarios

**Cons:**
- Different from original Windows logon goal
- May not meet user needs

## Recommendation

**Short term:** Update OpenMFA documentation to clarify:
1. Native Windows logon requires Active Directory
2. For standalone computers, recommend EIDAuthenticate
3. Provide setup guides for both scenarios

**Long term:** Consider Option 3 (custom credential provider) if there's significant demand for standalone computer logon without third-party dependencies.

## Technical Notes

### Why Our Certificates Are Correct But Don't Work

Our certificates have:
- ✅ Smart Card Logon EKU (1.3.6.1.4.1.311.20.2.2)
- ✅ Client Authentication EKU (1.3.6.1.5.5.7.3.2)
- ✅ UPN in Subject Alternative Name
- ✅ AT_SIGNATURE key specification
- ✅ Valid key pair on smart card
- ✅ Certificate in Windows Personal store
- ✅ Certificate on smart card (PKCS#15)

The issue isn't the certificates - it's that **Windows native credential provider doesn't support workgroup smart card logon**.

### What EIDAuthenticate Does Differently

EIDAuthenticate installs a custom credential provider that:
1. Replaces/extends the standard Windows logon UI
2. Implements its own certificate-to-user mapping logic
3. Authenticates against local SAM database (not Kerberos)
4. Injects authentication into lsass.exe (Windows security subsystem)

This bypasses the domain requirement entirely.

## Next Steps

1. **Test with EIDAuthenticate:**
   - Install EIDAuthenticate Community Edition (if on Windows Home)
   - Use our existing card with certificate
   - Verify it enables Windows logon

2. **Document the limitation:**
   - Update README with AD requirement
   - Add EIDAuthenticate recommendation for standalone
   - Provide clear setup paths for both scenarios

3. **Evaluate long-term strategy:**
   - Assess user demand for standalone logon
   - Consider credential provider development
   - Explore partnership opportunities with MySmartLogon

## Conclusion

Our OpenMFA smart card initialization and certificate management is **working correctly**. The "no valid certificates" error is due to a Windows architectural limitation, not our implementation.

For smart card logon on standalone Windows computers, users will need to install EIDAuthenticate or similar third-party credential provider software.
