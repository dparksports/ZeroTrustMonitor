# Technical Decisions Reference: ZeroTrustMonitor

This document serves as a comprehensive technical reference for all architectural and security decisions made during the development of the `ZeroTrustMonitor` CLI.

## 1. Universal vs. Targeted Scope
* **Decision:** Abandoned mandatory CLI arguments in favor of a universal, context-aware interactive menu.
* **Reasoning:** EDR hooks inherently carry high risk (potential memory access violations leading to process crashes). Global injection (`ALL` target) was removed to prevent accidental OS-wide instability. The tool now safely queries `Process.GetProcesses()` to list only active GUI applications, allowing surgical, targeted hook deployment.

## 2. Cryptographic Integrity over Metadata
* **Decision:** Upgraded the Retroactive Scanner to use SHA-256 hashing.
* **Reasoning:** Relying on the `LastWriteTime` property of files (e.g., `app.asar`) is highly vulnerable to Timestomping attacks. Malware can trivially manipulate file metadata via Win32 APIs like `SetFileTime`. Streaming the file and computing an SHA-256 hash ensures deterministic, cryptographically secure integrity verification against known-good baselines.

## 3. High-Capacity File System Monitoring
* **Decision:** Increased `FileSystemWatcher.InternalBufferSize` to the maximum `65536` bytes (64KB).
* **Reasoning:** The native .NET `FileSystemWatcher` is prone to dropping events during high-frequency I/O operations (Noise Attacks). Maximizing the buffer ensures burst-write operations (up to ~160 events/second) do not overflow the buffer and blind the sensor during a malware evasion attempt.

## 4. Native AOT Hook Payload (Architecture Hardening)
* **Decision:** Dropped 32-bit (x86) legacy support and compiled `AudioTrap.dll` as a pure 64-bit Native AOT library.
* **Reasoning:** Modern applications and Windows 11 are exclusively x64/ARM64. The .NET CLR imposes significant overhead and limitations when injected into unmanaged target processes. Native AOT (`<PublishAot>true</PublishAot>`) compiles the payload into raw machine code, allowing export of standard unmanaged `DllMain` entry points via `[UnmanagedCallersOnly]`.

## 5. Event Tracing for Windows (ETW) Integration
* **Decision:** Integrated Microsoft.Diagnostics.Tracing.TraceEvent to replace user-mode watcher limitations.
* **Reasoning:** `FileSystemWatcher` lacks process attribution (cannot identify the PID of the file creator). ETW (`KernelTraceEventParser.Keywords.FileIOInit` and `Process`) operates at Ring 0, providing real-time telemetry including exact Process IDs, Parent PIDs, and Command Lines. This enables active mitigation (auto-kill capabilities).

## 6. Global Process Hollowing Protection
* **Decision:** Implemented a system-wide ETW process monitor enforcing Parent-Child hierarchies.
* **Reasoning:** Malware abuses `svchost.exe` via Process Hollowing to evade firewalls and disguise malicious threads (keyloggers/screen scrapers). By hooking ETW `ProcessStart` events globally, the tool enforces known-good trees (e.g., `svchost.exe` must be spawned by `services.exe`) and catches malicious command-line arguments (e.g., PowerShell `-EncodedCommand`), neutralizing attacks before execution.
