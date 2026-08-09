# Global Tamper Protection for User Installed Applications

Yes, you **can** apply the tamper protection methods (like Exploit Protection and Controlled Folder Access) to all applications installed on your system. However, applying these globally requires careful configuration to avoid breaking legitimate software.

Here is how you can apply these protections globally or semi-globally across your system:

## 1. Global Exploit Protection (System Settings)

Windows Exploit Protection allows you to set baseline security rules that apply to *every single process* that starts on the system.

**How to configure globally:**
1. Open **Windows Security**.
2. Go to **App & browser control > Exploit protection settings**.
3. Under the **System settings** tab, you will see a list of mitigations.

**What you can safely set globally to ON:**
* **Data Execution Prevention (DEP):** Always ON.
* **Structured Exception Handling Overwrite Protection (SEHOP):** Always ON.
* **Bottom-Up ASLR:** Always ON.

**What you should NOT set globally (Use per-app instead):**
* **Arbitrary Code Guard (ACG):** If you set ACG to "On by default" globally, **every web browser (Chrome, Edge, Firefox), Electron app (Discord, Slack, VS Code), and Java application will immediately crash.** These apps require JIT compilation (generating dynamic code in memory) to function.
* **Export Address Filtering (EAF):** May break complex software or games that use anti-cheat engines.

*Recommendation:* Keep the system settings at their defaults, and use the **Program settings** tab to apply ACG and EAF strictly to native compiled applications that don't need dynamic memory (like custom C# apps, or traditional C++ software).

## 2. Global Smart App Control (SAC)

Smart App Control (SAC) is inherently a **global** system. Once enabled, it evaluates *every* executable, script, or DLL that attempts to run, regardless of where it is installed.

* If you enable SAC, it protects your entire system from untrusted software initiating execution or injecting code.
* *Constraint:* SAC must be enabled on a fresh installation of Windows 11.

## 3. Global Controlled Folder Access (Ransomware Protection)

Controlled Folder Access (CFA) is designed to protect specific folders from unauthorized modification by *any* untrusted app.

**How to protect all installed apps:**
By default, CFA protects your User Documents, Pictures, and Desktop. You can extend this to protect your program installation directories.
1. Open **Windows Security > Virus & threat protection > Ransomware protection > Manage ransomware protection**.
2. Turn ON **Controlled folder access**.
3. Click **Protected folders**.
4. Add the following directories to protect all installed software from being tainted (e.g., malware modifying an `.asar` file or dropping a rogue `.dll`):
   - `C:\Program Files\`
   - `C:\Program Files (x86)\`
   - `C:\Users\<YourUsername>\AppData\Local\Programs\` (Where many Electron apps install themselves).

**The Trade-off:**
If you protect `C:\Program Files\`, legitimate application updaters might get blocked when they try to install a new version. You will need to click **Allow an app through Controlled folder access** in Windows Security to manually authorize installers or updaters when they run.

## 4. AppLocker / Windows Defender Application Control (WDAC)

If you want the ultimate global protection, WDAC forces the entire OS to run only trusted code.
* You can deploy a WDAC policy that says: *"Only allow applications to run if they are installed in C:\Program Files\ AND the folder requires Administrator rights to write to."*
* Since a user-level worm or rogue script does not have Administrator rights, it cannot write to `Program Files`. Therefore, it cannot taint the apps, and any script it drops elsewhere will be blocked from executing by WDAC.
