# Tracking the Source of a Noise Attack

If malware attempts a "Noise Attack" by rapidly creating thousands of temporary files to overwhelm the `FileSystemWatcher`, our CLI will detect the massive flood of events. However, `FileSystemWatcher` has one major limitation: **It tells you *what* file was created, but it does *not* tell you *which process* created it.**

To track down the rogue script or malware causing the noise attack, we must look beyond `FileSystemWatcher` and use advanced Windows telemetry. Here are the four best ways to catch the attacker:

## 1. Sysmon (System Monitor) - The Industry Standard
Sysmon is a free, advanced security tool from Microsoft Sysinternals. When installed, it logs highly detailed system activity directly to the Windows Event Viewer.
* **How it works:** Sysmon operates as a Kernel driver.
* **What to look for:** We would configure Sysmon to monitor the target directory (e.g., `C:\Program Files\Reolink\`). When the noise attack happens, Sysmon will generate **Event ID 11 (FileCreate)** for every file dropped.
* **The Catch:** Sysmon's event log will contain the exact **Process ID (PID)**, the **Image Path** (e.g., `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`), and the user account that ran the script!

## 2. Event Tracing for Windows (ETW) - The Developer Method
ETW is the built-in, high-speed logging framework inside the Windows OS.
* **How it works:** Our C# CLI could be upgraded to subscribe to the `Microsoft-Windows-Kernel-File` ETW provider.
* **The Catch:** Instead of just getting a generic "File Created" event, ETW streams real-time data that includes the exact Process ID that requested the file creation. Our CLI could instantly read the PID, look up the process name, and automatically kill it.

## 3. Windows Security Auditing (Built-in but Noisy)
Windows has a built-in auditing system, but it is turned off by default because it generates massive amounts of logs.
* **How it works:** You can use the Windows Group Policy Editor to enable **"Audit File System"**, and then right-click the Reolink folder -> Properties -> Security -> Advanced -> Auditing, and add a rule to audit "Create files".
* **The Catch:** The noise attack will generate **Event ID 4663** in the Windows Security Log. The log details will include the "Process Name" (e.g., `cmd.exe`) that created the files.

## 4. Kernel Minifilter Driver (The EDR Method)
This is how professional Antivirus and EDR products work.
* **How it works:** Instead of relying on user-mode watchers, the security company writes a C/C++ driver that sits inside the Windows Kernel file system stack.
* **The Catch:** Before a file is even allowed to be written to the hard drive, the request passes through the Minifilter. The filter checks the calling Process ID. If it sees a single process trying to create 1,000 files a second, it simply blocks the requests with an "Access Denied" error and instantly terminates the rogue process.
