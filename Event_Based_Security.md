# Event-Based Security vs. Polling

The user asked: *"How do these security measurements work? Do they periodically examine each app folder? Can we make the CLI event-based so it doesn't loop periodically, but knows the moment it has been tampered with?"*

## 1. How Windows Built-in Security Works (It is already Event-Based!)
None of the advanced Windows security features (Smart App Control, Arbitrary Code Guard, Controlled Folder Access) use "polling" or periodic looping. Polling (e.g., checking a folder every 5 seconds) is terrible for performance and leaves a 5-second window where malware could do its damage.

Instead, Windows security features are entirely **Event-Based** at the Kernel level:

* **Controlled Folder Access (CFA):** Windows uses a "File System Mini-Filter Driver." This sits between the hard drive and the operating system. When a program asks to write to `C:\Program Files\`, the request physically cannot reach the hard drive without passing through the filter. The filter evaluates the event *instantly*. If the program is unauthorized, the filter blocks the write operation before a single byte hits the disk.
* **Arbitrary Code Guard (ACG):** When a program calls the `VirtualAlloc` API to create dynamic memory, the Windows Memory Manager evaluates the request. If ACG is on, the OS denies the API call instantly.
* **Smart App Control (SAC):** Before the Windows Kernel creates a new process, it checks the file's signature. If it fails, the process is never created.

## 2. Making Our CLI Tool Event-Based
Currently, our `ZeroTrustMonitor` CLI tool runs once, does a point-in-time scan (polling), and exits. 

To make our own tool **event-based** so that it sits silently in the background (0% CPU usage) and alerts you the exact millisecond an Electron app's folder is tampered with, we can use the **`FileSystemWatcher`** class in C#.

### The `FileSystemWatcher` Approach
`FileSystemWatcher` hooks into the Windows OS file system notifications.
1. We tell it to watch the `C:\Program Files\Reolink` folder.
2. The program goes to sleep.
3. If a worm drops a malicious `version.dll` into that folder, or modifies the `app.asar` file, Windows instantly wakes up our program and triggers the `OnChanged`, `OnCreated`, or `OnDeleted` event.
4. Our tool instantly logs the tampering and alerts the user.

This is exactly how professional EDR (Endpoint Detection and Response) tools monitor file integrity without killing your CPU!
