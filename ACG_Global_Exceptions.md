# Applying ACG Globally with Exceptions

The user asked: *"Can we enable ACG for all apps installed by users with the exceptions of all browsers, VS Code, Java, and any code-based apps?"*

**The short answer is: Technically yes, but practically, it is a nightmare to maintain and highly discouraged by Microsoft.**

Here is a breakdown of why this approach is problematic and how Windows Exploit Protection is actually designed to be used.

## 1. The "Exception List" is Too Large
If you turn ACG (Arbitrary Code Guard) "On by default" in System Settings, you must create a manual exception for *every single application* that uses JIT. 
This list is not just "Chrome and VS Code". It includes:
* **All web browsers** (Chrome, Edge, Firefox, Brave, Opera, Safari)
* **All Electron apps** (Discord, Slack, Teams, WhatsApp, Spotify, GitHub Desktop, Bitwarden, Figma, Notion)
* **All Java apps** (Minecraft, IntelliJ, Eclipse, enterprise tools)
* **All .NET (C#/VB) Desktop Apps** (This is thousands of common Windows tools, utilities, and enterprise apps).
* **Game Launchers and Anti-Cheats** (Steam, Epic Games, Riot, Vanguard, BattlEye)
* **Graphics Drivers** (Some NVIDIA/AMD control panels use JIT UI frameworks).

If you miss even one app, it will instantly crash upon launch. When an app updates to a new architecture, you might have to update the rule.

## 2. Windows Exploit Protection Design Philosophy
Microsoft designed Exploit Protection with the exact **opposite** philosophy: **"Opt-In for Native Apps" rather than "Opt-Out for JIT Apps."**

Because the vast majority of modern consumer software utilizes JIT compilation or dynamic memory in some capacity, turning ACG on globally breaks the Windows ecosystem. 

Instead, the intended usage is:
1. Leave ACG **Off by default** globally.
2. Go to the **Program settings** tab.
3. Explicitly add the specific, high-risk native applications that you *know* are AOT-compiled (C/C++/Rust) and apply ACG to them. 

Examples of apps you *should* explicitly apply ACG to:
* High-risk network parsers (like custom native web servers).
* Native PDF readers (like SumatraPDF or Adobe Acrobat, if supported).
* The custom Native C# AOT Reolink app we discussed building.

## 3. How to technically do it (If you really want to)
If you still want to force this "Global ON, specific OFF" policy, you would have to write a complex PowerShell script to deploy the XML configuration.

1. **Export current settings:** `Get-ProcessMitigation -RegistryConfigFilePath "C:\mitigations.xml"`
2. **Edit the XML:** You would have to manually edit the XML file. You would set the system-wide ACG flag to ON.
3. **Add Exceptions:** You would then have to add an `<AppConfig>` block for *every single JIT executable* on your system, setting ACG to OFF for that specific `.exe`.
4. **Import:** `Set-ProcessMitigation -PolicyFilePath "C:\mitigations.xml"`

**Warning:** If you do this and forget to add `explorer.exe` or a critical system UI component that relies on .NET/XAML, you could potentially soft-brick your Windows UI upon reboot.
