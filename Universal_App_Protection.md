# Universal App Protection vs. Targeted Protection

The user asked: *"Can we protect all user-installed apps automatically, or do users have to designate one app at a time?"*

The short answer is: **Yes, we can protect all apps automatically, but the method depends on the type of security protocol.**

Here is how "Universal" vs "Targeted" protection works for each security layer:

## 1. Global OS Mitigations (Universal by Default)
Some security features are inherently universal. You do not need to target specific apps; they protect the entire operating system.
* **Controlled Folder Access (Ransomware Protection):** When activated, it universally protects `C:\Program Files\` and user directories. Every single installed app is automatically shielded from file tampering.
* **Smart App Control (SAC):** Universally blocks any unsigned or untrusted executable from launching on the system.
* **Memory Integrity (HVCI):** Universally protects the Windows Kernel from all malicious drivers.

## 2. Retroactive Scanning (Can be Universal)
We can absolutely update our CLI to scan *all* installed apps. 
Instead of taking a single app folder (like `C:\Program Files\Reolink`), the tool can recursively scan `C:\Program Files\`, `C:\Program Files (x86)\`, and `C:\Users\<User>\AppData\Local\` for suspicious `.dll` hijacking or tampered `.asar` files. 
* *Trade-off:* Scanning the entire hard drive takes several minutes instead of a few seconds.

## 3. Real-Time Event Monitoring (Can be Universal)
We can set the `FileSystemWatcher` to monitor the root `C:\` drive or `C:\Program Files\`. 
* *Trade-off:* The tool will receive thousands of events per second as Windows naturally creates and deletes temporary files. The tool must have a highly optimized filter to ignore normal OS noise and only alert on actual tampering, or it will max out the CPU.

## 4. EDR API Hooking (Requires Universal Injection)
To catch API calls (like audio muting) across *all* apps, a user-mode EDR tool must loop through every single running process on the computer (`Process.GetProcesses()`) and inject the `AudioTrap.dll` into all of them.
* *This is exactly what professional EDRs (like CrowdStrike) do.*
* *Trade-off:* Injecting code into every running app is highly intrusive. If the hook has a bug, it will crash every program on the computer simultaneously. This is why EDRs require extreme stability testing.

## Making the CLI Convenient
To make the `ZeroTrustMonitor` user-friendly, we can remove the requirement to type `electron` or `reolink` in the command line. Instead, if the user just double-clicks the `.exe`, the CLI can automatically default to a **"Universal System Scan"** mode, finding and evaluating all running processes!
