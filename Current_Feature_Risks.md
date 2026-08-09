# What Could Go Wrong With Current Features?

Even though our `ZeroTrustMonitor` CLI is getting powerful, it is still a "User-Mode" tool. Because it lives in the same space as the apps it is monitoring, it has a few Achilles heels that advanced malware could exploit:

## 1. Retroactive Scan (Timestomping)
* **The Flaw:** Currently, our scanner looks at the `LastWriteTime` of the `app.asar` file to see if it was modified recently. 
* **The Exploit:** Malware uses a technique called "Timestomping". After injecting malicious code into the `.asar` file, the malware simply copies the "Last Modified Date" from a legitimate Windows file and overwrites the `.asar` file's metadata. Our CLI will look at the date, see it is from 2023, and assume the file is safe.
* **The Fix:** We must calculate the **SHA-256 Hash** of the file contents and compare it against a known-good database, completely ignoring the file date.

## 2. Event-Based File Monitor (Buffer Overflow)
* **The Flaw:** We are using the native .NET `FileSystemWatcher`.
* **The Exploit:** If malware wants to temper with Reolink but knows our watcher is running, it can launch a "Noise Attack". It rapidly creates and deletes 100,000 temporary files in the Reolink directory in a single second. The `FileSystemWatcher` has a small internal memory buffer. When flooded with events, the buffer overflows, the watcher crashes or drops events, and the malware quietly modifies the real file during the chaos.
* **The Fix:** We need to increase the `InternalBufferSize` of the watcher, or better yet, build a Kernel "Mini-Filter Driver" that can pause file writes before they happen.

## 3. EDR API Hooking (Architecture Mismatch & PPL)
* **The Flaw:** Our `DllInjector` uses `CreateRemoteThread`.
* **The Exploit (Architecture):** You cannot inject a 64-bit DLL into a 32-bit application, and vice versa. If Reolink is a 32-bit app and our CLI is 64-bit, the injection fails.
* **The Exploit (PPL):** If the malware runs inside a Windows "Protected Process Light" (PPL) container (like an Antivirus or System process), the Windows Kernel will reject `OpenProcess(PROCESS_ALL_ACCESS)`. Our CLI will get an "Access Denied" error and fail to inject the hook.

## 4. Kernel Driver Audit (Catalog Signatures)
* **The Flaw:** We are using `WinVerifyTrust` to check the `.sys` file directly.
* **The Exploit:** Many legitimate Windows drivers do not have the signature embedded directly inside the `.sys` file. Instead, their signature is stored in a separate `.cat` (Catalog) file in the Windows System32 directory. Our current check might flag legitimate Microsoft drivers as "Unsigned" (False Positives) because it doesn't know how to search the Catalog database!
