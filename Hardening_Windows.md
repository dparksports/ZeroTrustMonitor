# Hardening Windows 11 Against Process Injection

While there is no single "Turn off process injection" button in Windows (because it would break the OS and many applications), Microsoft provides several powerful, opt-in security features that users can configure to severely restrict memory injection and malicious behavior.

Here are the ways a user can harden Windows 11 to block these attacks:

## 1. Windows Exploit Protection
Exploit Protection is a built-in feature of Windows Security that allows you to apply strict memory and process mitigation policies. You can apply these system-wide or to specific applications (like your Electron app).

**How to enable it:**
1. Open **Windows Security**.
2. Go to **App & browser control** > **Exploit protection settings**.
3. You can configure **System settings** or **Program settings** (to target a specific `.exe`).

**Key mitigations to block injection:**
* **Arbitrary code guard (ACG):** Prevents a process from generating dynamic code or modifying executable code segments. This stops most classic shellcode injections.
* **Export address filtering (EAF):** Blocks malware from looking up where critical system functions (like `LoadLibrary` or `VirtualAlloc`) are located in memory, breaking the malware's ability to inject DLLs.
* **Block low integrity images:** Prevents the app from loading DLLs that were dropped by a low-integrity process (like a web browser).
* **Block remote images:** Prevents the app from loading DLLs over a network share.

## 2. Smart App Control (SAC)
Introduced in Windows 11, Smart App Control is a major security enhancement that blocks malicious, untrusted, or unsigned apps and scripts from running. 
* Rather than trying to stop injection after the malware is running, SAC prevents the rogue process or script from executing in the first place.
* **How to enable:** Go to **Windows Security > App & browser control > Smart App Control settings**. *(Note: This can only be enabled on a fresh installation of Windows 11 to guarantee the system isn't already compromised).*

## 3. Windows Defender Application Control (WDAC) / AppLocker
For advanced users or enterprise environments, WDAC is the ultimate defense. It is an "Allowlist" system.
* You configure Windows to *only* run applications and scripts that are cryptographically signed by trusted publishers (e.g., Microsoft and the vendor of your Electron app).
* If a rogue, unsigned executable or script attempts to run, the Windows Kernel blocks it immediately. If the rogue process cannot start, it cannot inject threads.

## 4. Controlled Folder Access
If the malware is attempting to taint your Electron app on disk (e.g., modifying `app.asar` or dropping a rogue DLL), you can use Controlled Folder Access.
* This feature blocks unauthorized applications from modifying files in protected directories.
* **How to enable:** Go to **Windows Security > Virus & threat protection > Ransomware protection > Manage ransomware protection**. Turn on **Controlled folder access** and add your Electron app's installation folder to the protected list.

## 5. Applying Mitigations via PowerShell
PowerShell users can apply strict process mitigations to specific executables using the `Set-ProcessMitigation` cmdlet. 
For example, to force an Electron app to strictly validate image signatures and block dynamic code:

```powershell
Set-ProcessMitigation -Name "electron_app.exe" -Enable DisableDynamicCode
Set-ProcessMitigation -Name "electron_app.exe" -Enable StrictHandle
```
*Note: Applying these restrictions to an Electron app might break it, because modern JavaScript engines (V8) rely on Dynamic Code Generation (JIT compilation) to run efficiently. You must test these policies carefully.*
