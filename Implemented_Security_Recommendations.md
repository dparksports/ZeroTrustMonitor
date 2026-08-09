# Implemented Security Recommendations

This document outlines the security recommendations that have been successfully implemented to harden the `ZeroTrustMonitor` CLI against advanced evasion techniques.

## 1. SHA-256 Cryptographic Hashing (Defeating Timestomping)
**The Vulnerability:** 
Initially, the retroactive scanner checked the `LastWriteTime` of target files (like Electron's `app.asar`). Advanced malware utilizes a technique called "Timestomping"—copying the timestamps from legitimate Windows files and applying them to the tampered files to make them appear unmodified.

**The Implementation:** 
We abandoned timestamp verification and implemented **SHA-256 Cryptographic Hashing**. The CLI now reads the raw byte stream of the target file and calculates its cryptographic signature. Even if malware flips a single bit and fakes the timestamp, the hash will change entirely, instantly triggering a tamper alert.

## 2. Buffer Overflow Optimization (Defeating Noise Attacks)
**The Vulnerability:** 
The real-time monitor uses `FileSystemWatcher`. If malware attempts to tamper with a monitored file, it could simultaneously generate hundreds of thousands of dummy files (a "Noise Attack"). This would overflow the default memory buffer of the watcher, causing it to drop events and miss the actual tamper event.

**The Implementation:** 
We explicitly increased the `InternalBufferSize` of the `FileSystemWatcher` to its maximum allowed limit of `65536` bytes (64KB). This ensures the sensor can queue massive bursts of file system events without crashing or dropping critical alerts during an evasion attempt.

## 3. 64-Bit Exclusive Native AOT Payload (Architecture Hardening)
**The Vulnerability:** 
Attempting to maintain legacy 32-bit (x86) injection support introduces unnecessary complexity and potential attack surfaces into the EDR hooking module.

**The Implementation:** 
As approved, we dropped all support for 32-bit architecture. Modern Windows 11 and modern applications (like Reolink) are exclusively 64-bit. We built the `AudioTrap` payload as a pure **Native AOT (Ahead-Of-Time)** 64-bit library. This removes the need for the .NET runtime in the target application and allows our payload to be injected seamlessly using standard unmanaged `DllMain` entry points.

## 4. Kernel Driver Catalog Signatures (Pending / Acknowledged Risk)
**The Vulnerability:** 
Our current Kernel Driver Audit uses `WinVerifyTrust` directly on the `.sys` file. However, some legitimate Microsoft drivers do not contain embedded signatures; their signatures are stored externally in `.cat` (Catalog) files. This can lead to false positives where legitimate drivers are flagged as "Unsigned."

**Next Steps:** 
In future iterations, the driver auditing logic should be expanded to query the Windows Cryptographic Catalog Database (`crypt32.dll`) to verify external signatures.
