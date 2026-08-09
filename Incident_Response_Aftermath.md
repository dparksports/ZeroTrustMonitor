# Incident Response: The Aftermath of Detection

When the ZeroTrustMonitor (or any advanced EDR) detects a Hollowed Process (unbacked executable memory), detecting it is only half the battle. If you simply kill the process and do nothing else, the malware will likely just respawn itself 5 seconds later. 

Here is exactly how a professional EDR addresses the aftermath of a Process Hollowing detection.

## Phase 1: Immediate Mitigation (The Kill)
The absolute first priority is stopping the bleeding. 
* **Action:** The EDR immediately calls `Process.Kill()` on the target PID.
* **Why:** If the malware is actively keylogging, taking screenshots, or exfiltrating data, every millisecond counts. Terminating the process instantly cuts off its access to the OS and severs its network connection to the hacker.

## Phase 2: Forensic Memory Dumping (Capture the Payload)
Before the process is entirely destroyed, or immediately upon detection, we want to steal the hacker's weapon.
* **Action:** The EDR uses the `ReadProcessMemory` API to copy the exact bytes of the Unbacked `MEM_PRIVATE` memory and saves it to a file on the hard drive (e.g., `Malware_Dump_PID4892.bin`).
* **Why:** Malware on the hard drive is usually heavily encrypted and packed, making it hard to analyze. But to run in memory, *the malware must decrypt itself*. By dumping the unbacked memory, we capture the pure, unencrypted malicious payload. Security analysts can reverse-engineer this to find out exactly what the malware was trying to do.

## Phase 3: Tracing the Infection Vector (Killing the Parent)
Malware rarely acts alone. If we caught a hollowed `svchost.exe`, something else on the computer created it.
* **Action:** The EDR queries the Parent PID of the hollowed process. If the parent is `powershell.exe`, `cmd.exe`, or a rogue script like `update.vbs`, the EDR instantly terminates the parent process as well.
* **Why:** If you kill the hollowed process but leave the script that spawned it running, the script will just launch a brand new hollowed process. You have to kill the "Dropper."

## Phase 4: Hunting for Persistence (Preventing the Respawn)
Hackers want their malware to survive when you restart your computer. To do this, they establish "Persistence."
* **Action:** The EDR automatically kicks off a scan of the most common Windows persistence locations:
  1. **Registry Run Keys:** `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
  2. **Scheduled Tasks:** `schtasks.exe` (Hackers love creating tasks that run every 5 minutes).
  3. **Startup Folder:** `AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup`
* **Why:** The EDR must delete these persistence mechanisms. If it doesn't, the malware will reinfect the machine the moment the user reboots.

## Phase 5: Network Quarantine
* **Action:** In an enterprise environment, the EDR will instruct the Windows Firewall to drop all inbound and outbound network traffic for the entire computer, *except* for the secure channel back to the EDR console.
* **Why:** This isolates the infected machine from the rest of the company network, preventing the malware from spreading laterally (like a worm) to other computers while the IT team investigates the memory dump.
