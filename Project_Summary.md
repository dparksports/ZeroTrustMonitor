# Project Summary and Troubleshooting

This document serves as a master reference for all the tools, scripts, and documentation generated to combat rogue process injection and audio manipulation.

## Generated Documentation

1. **[Recommendations.md](./Recommendations.md)**: Manual diagnostic steps to detect a worm manipulating an Electron app's volume.
2. **[EDR_Concepts.md](./EDR_Concepts.md)**: Detailed explanations of how API hooking freezes threads (using `INT 3` breakpoints), and how EDRs use Stack Walking and Register Inspection to identify malicious callers.
3. **[Injection_and_Monitoring.md](./Injection_and_Monitoring.md)**: Explains how malicious thread injection works (e.g., `CreateRemoteThread`) and how to detect it using memory scanners.
4. **[Security_and_Tainting.md](./Security_and_Tainting.md)**: Details the Windows security model regarding process injection and how to verify if an Electron app has been tainted on disk (ASAR modification, shortcut hijacking, etc.).
5. **[Hardening_Windows.md](./Hardening_Windows.md)**: Outlines built-in Windows 11 opt-in features (Exploit Protection, Smart App Control, Controlled Folder Access) to harden the OS against injection attacks.
6. **[Native_Reolink_Architecture.md](./Native_Reolink_Architecture.md)**: Architectural design for building an open-source C# Native Reolink app to eliminate Electron's massive overhead and allow for strict memory mitigations.

## Developed Tools

1. **Audio Troubleshooter CLI (`Program.cs`)**:
   - A complete C# .NET 10 command-line tool.
   - Automatically scans for muted audio sessions belonging to the target app (e.g., "reolink").
   - Flags suspicious DLLs injected into the process space.
   - Verifies the system's security posture (Smart App Control status and Ransomware Protection).
2. **Custom OS Debugger (`CustomApiMonitor.cs`)**:
   - Source code demonstrating how an OS-level Debugger attaches to a process to intercept thread events natively.
3. **Inline API Hook Payload (`MinHookPayload.cs`)**:
   - A conceptual C# payload using MinHook.NET to intercept the `SetMute` API inline without the heavy performance penalty of a full debugger.

## Known Issues and Troubleshooting

### Build Error: File Locked
If you attempt to run `dotnet build` and receive an error stating that the process cannot access `AudioTroubleshooter.exe` because it is being used by another process:

**Cause:** You currently have the `AudioTroubleshooter.exe` running in a command prompt or PowerShell window. Windows locks executable files while they are running, preventing the compiler from overwriting them with a newly built version.

**Solution:**
1. Switch to the terminal window where the tool is running.
2. Press any key to exit the application, or close the terminal window completely.
3. Run `dotnet build` again. It will succeed now that the file is unlocked.
