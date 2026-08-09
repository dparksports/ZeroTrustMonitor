# How Malware Abuses Svchost.exe

The `svchost.exe` (Service Host) process is a critical Windows component used to host multiple background Windows services. Because a normal Windows PC naturally has 50 to 80 `svchost.exe` processes running at all times, and because they naturally connect to the internet (for Windows Updates, time sync, etc.), it is the perfect camouflage for malware.

Here is exactly how a rogue script uses it to keylog, exfiltrate, or screen-watch, and how we detect it.

## 1. How Malware Hijacks Svchost (The Attack)

Malware rarely creates a malicious script named `keylogger.exe`, because Antivirus will instantly flag it. Instead, they use a technique called **Process Hollowing**:
1. **Spawn:** The malware quietly asks Windows to launch a legitimate, perfectly safe copy of `C:\Windows\System32\svchost.exe` in a "Suspended" state.
2. **Hollow:** The malware forces the suspended `svchost.exe` to un-map (delete) its own legitimate Microsoft code from its memory.
3. **Inject:** The malware injects its malicious code (the keylogger) into the empty memory space of `svchost.exe`.
4. **Resume:** The malware resumes the process. 

To the Windows Task Manager and standard Antivirus, it looks exactly like a legitimate Microsoft `svchost.exe` process signed by Microsoft.

### A. Keylogging & Screen Watching
Once hiding inside `svchost.exe`, the malware uses standard Windows APIs:
* **Keylogging:** It calls `SetWindowsHookEx(WH_KEYBOARD_LL)` to register a global hook. Every time you type, Windows passes the keystroke directly into the hollowed `svchost.exe`.
* **Screen Watching:** It calls GDI APIs like `GetDC(NULL)` and `BitBlt` to take screenshots of the desktop every few seconds.

### B. Data Exfiltration
This is why `svchost.exe` is so dangerous. If `keylogger.exe` tries to upload a file to a Russian IP address, the Windows Firewall will block it and alert the user. 
But because `svchost.exe` is the official Windows process responsible for downloading Windows Updates, the Firewall has a permanent rule to allow `svchost.exe` to talk to the internet! The malware can simply open a TCP socket and upload your passwords and screenshots entirely unblocked.

---

## 2. How to Detect the Hijack (The Defense)

While Process Hollowing is sneaky, it leaves several massive forensic footprints that an advanced EDR (like our ZeroTrustMonitor) can detect:

### Detection 1: The Parent Process Anomaly (The Easiest Catch)
* **Normal Behavior:** Legitimate `svchost.exe` processes are *only* ever spawned by one master process: `services.exe` (The Windows Service Control Manager).
* **The Catch:** If you check the Process Tree and see an `svchost.exe` whose parent is `powershell.exe`, `cmd.exe`, `word.exe`, or `reolink.exe`, it is 100% confirmed malware.

### Detection 2: The Command Line Anomaly
* **Normal Behavior:** When `services.exe` launches a real Service Host, it must tell it which service group to load. Therefore, real `svchost` processes *always* have arguments like: `svchost.exe -k netsvcs` or `svchost.exe -k LocalService`.
* **The Catch:** Malware often gets lazy and simply spawns `svchost.exe` with no arguments at all. An `svchost.exe` running with a blank command line is an instant red flag.

### Detection 3: Unbacked Memory (The Advanced Catch)
* **Normal Behavior:** In a real program, the executable memory (where the code lives) is physically "backed" by a file on the hard drive (e.g., the memory points back to `C:\Windows\System32\svchost.exe`).
* **The Catch:** Because the malware hollowed out the real file and injected its own raw code directly into RAM, the new malicious memory space has no file backing it on the hard drive. An EDR can scan the RAM of all `svchost` processes, and if it finds a block of executable memory that doesn't map back to a file, it instantly kills the process.
