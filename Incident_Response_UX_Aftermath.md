# Incident Response & Scanning UX (Aftermath Protocols)

This document details the newly implemented User Experience (UX) for scanning processes and handling the aftermath of a Process Hollowing detection in the ZeroTrustMonitor.

## 1. Timeframe-Based Automatic Scanning UX
When users suspect a rogue script or process executed recently, asking them to manually pick a Process ID from a list of 300 active programs is tedious and prone to human error. We overhauled this process to make it fully automatic based on a "Suspected Time of Infection."

### The New Flow:
1. The user selects **Option 3: Scan processes started within a specific timeframe**.
2. They are presented with a simple sub-menu:
   * `[1] Last 10 minutes`
   * `[2] Last 30 minutes`
   * `[3] Last 1 hour`
   * `[4] Today (Since midnight)`
   * `[5] Since last startup (Boot time)`
3. The CLI queries the Windows Kernel for the exact `StartTime` of all active processes.
4. It filters out everything that doesn't match the timeframe and automatically executes the Memory Scanner (`VirtualQueryEx`) on the matching subset, requiring absolutely zero manual PID input from the user.

## 2. The Automated Aftermath Protocol
If the scanner detects Unbacked Executable Memory (meaning a process has been hollowed out or injected with malicious code), the CLI automatically kicks off the 5-Phase Incident Response Aftermath Protocol. 

To ensure the user is fully aware of what the security tool is doing to their system, the CLI prints a live, step-by-step audit log of the mitigation process:

### Phase 1 & 2: Detection and Dumping
```text
[!!!] PROCESS HOLLOWING DETECTED IN svchost (PID: 8821)!
      -> Executable memory found with NO backing file (Unbacked Memory)!

[+] Initiating Incident Response Aftermath Protocol...
    -> [Phase 2] Dumping unbacked memory payload...
       [SUCCESS] Payload dumped to: Malware_Dump_PID8821_0x1A4000.bin
```
The CLI uses `ReadProcessMemory` to rip the decrypted malicious payload directly out of the hollowed process's RAM and saves it to a `.bin` file on the hard drive for forensic analysis.

### Phase 3 & 1: Tracing Origin and Termination
```text
    -> [Phase 3] Tracing origin (Parent Process)...
       [SUCCESS] Found Parent PID 1042 (powershell). Terminating parent...
       [SIMULATED] Parent Process 1042 terminated.
    -> [Phase 1] Terminating hollowed process...
       [SIMULATED] Hollowed Process 8821 terminated.
```
The CLI queries WMI (`Win32_Process`) to find the parent process (the "Dropper"). It terminates both the parent script and the child hollowed process to prevent respawning. *(Note: Kills are simulated during testing to prevent accidental system crashes).*

### Phase 4 & 5: Persistence and Quarantine
```text
    -> [Phase 4] Hunting for Persistence (Registry/Startup)...
       [WARNING] Found 3 startup registry keys. Please review them.
    -> [Phase 5] Network Quarantine...
       [SIMULATED] Windows Firewall instructed to block all traffic for this host.
```
The CLI queries the `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` registry hive to warn the user if the malware attempted to survive a system reboot. Finally, it initiates a simulated network quarantine.

This transparent UX ensures the user is protected while understanding exactly how the tool neutralized the threat.
