# Thread Injection and API Monitoring

## Thread Injection: How it Works and How to Detect it

A common technique for malware to hide its actions is to inject malicious code into a legitimate, already-running process (like `explorer.exe` or `svchost.exe`). This way, when the malicious code executes, it appears to be coming from a trusted application.

### The Injection Process (Classic DLL Injection)
1. **OpenProcess:** The rogue malware gets a handle to a target process (e.g., an Electron app) with extensive permissions (`PROCESS_ALL_ACCESS`).
2. **VirtualAllocEx:** It allocates memory space *inside* the target process.
3. **WriteProcessMemory:** It writes the path of its malicious DLL (or the raw malicious code itself) into that newly allocated memory.
4. **CreateRemoteThread:** It forces the target process to start a new thread. The starting address of this thread is set to the `LoadLibrary` API function, and the argument passed to it is the memory address containing the path to the malicious DLL.
5. **Execution:** The target process unknowingly loads the malicious DLL, executing the malware's code within the context of the trusted application.

### How to Detect Thread Injection
Finding injected threads after the fact is challenging but possible using memory analysis.
1. **Memory Scanners (Process Explorer / Process Hacker):** 
   - These tools can scan the memory regions of a process.
   - They look for memory pages that are marked as **Executable (PAGE_EXECUTE_READWRITE)** but are *not* backed by a legitimate file on disk (an image). Normal executable code is backed by an `.exe` or `.dll` file. Injected code often resides in unbacked memory allocations.
2. **Thread Start Addresses:**
   - Inspecting the threads of a process (e.g., in Process Explorer).
   - A legitimate thread typically starts within the address space of the main `.exe` or a known system `.dll`.
   - An injected thread might have a start address pointing to a dynamically allocated memory region (unbacked by a file) or an unusual DLL.
3. **EDR/Sysmon (Real-time Detection):**
   - Sysmon Event ID 8 (`CreateRemoteThread`) logs when one process creates a thread in another process. This is highly suspicious if the source process is unknown or the target is a critical system process.

## Is there a simple way to detect calling of *any* Audio API?

The simplest built-in way without installing complex hooking software is to use **Windows Event Tracing (ETW)** or simply monitoring the **Windows Event Viewer**.

### The Event Viewer Approach
The Windows Audio service logs events when applications request audio sessions.
1. Open **Event Viewer**.
2. Navigate to `Applications and Services Logs -> Microsoft -> Windows -> Audio -> Operational` (You may need to enable this log first by right-clicking it and selecting "Enable Log").
3. This log can record events when applications connect to the audio service. However, it might not log every single volume change out of the box, as that would generate too much noise.

### The ETW Approach (Programmatic)
Using Event Tracing for Windows (ETW), you can subscribe to the `Microsoft-Windows-Audio` provider. This requires writing a script or application (like using the `Microsoft.Diagnostics.Tracing.TraceEvent` library in C#) to listen to the kernel-level events emitted by the audio subsystem. This is lightweight but requires development effort.

To catch the malware "red-handed" and freeze it, as discussed previously, **API Hooking** (using a tool like API Monitor) is the only way to intervene *before* the action completes.
