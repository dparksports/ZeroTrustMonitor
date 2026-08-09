# Scanning Memory for Process Hollowing (Unbacked Memory)

Process Hollowing is a technique where malware launches a legitimate process (like `svchost.exe`), hollows out its legitimate Microsoft code, and injects malicious code in its place. 

While checking the Parent-Child process tree (e.g., ensuring `svchost.exe` is spawned by `services.exe`) is the easiest way to detect this, advanced malware might hollow out an app that you intentionally launched yourself. To catch this, an EDR must scan the actual **RAM (Memory)** of the running process.

Here is the technical concept of how to scan memory and verify a process is not hollowed out:

## The Concept: "Backed" vs. "Unbacked" Memory

When Windows launches a legitimate program, it uses a system called **Memory Mapping**. 
1. Windows reserves memory (RAM) for the program's executable code.
2. It sets the permissions on that memory to `PAGE_EXECUTE_READ`.
3. It "backs" that memory to the physical file on the hard drive (e.g., the RAM points directly to `C:\Windows\System32\svchost.exe`). 
This is called **MEM_IMAGE** memory.

When Malware performs Process Hollowing, it does this:
1. It un-maps (deletes) the legitimate `MEM_IMAGE` memory.
2. It allocates brand new memory for its payload using `VirtualAllocEx`.
3. Because this new memory was created out of thin air, it has no file associated with it on the hard drive. 
This is called **MEM_PRIVATE** (Unbacked) memory.

## How to Scan for Unbacked Memory in C#

To detect hollowing, our C# EDR would loop through the memory pages of a running process using Windows APIs and look for executable code that shouldn't be there.

### Step 1: Query the Memory Pages (`VirtualQueryEx`)
We use a native Windows API called `VirtualQueryEx`. This function allows us to ask the Windows Kernel for a map of every single memory block inside the target process (e.g., `svchost.exe`).

### Step 2: Look for Executable Code
Most memory inside a program is just data (variables, text). We don't care about that. We only care about memory where the CPU is allowed to run code. We filter the memory blocks and only look at pages marked as:
* `PAGE_EXECUTE_READ`
* `PAGE_EXECUTE_READWRITE` (Highly suspicious on its own!)

### Step 3: Check if the Memory is "Backed" (`GetMappedFileName`)
For every block of executable memory we find, we call another native Windows API: `GetMappedFileName` (or `QueryVirtualMemory`). 
This API asks the OS: *"What file on the hard drive does this memory belong to?"*

### Step 4: The Detection (The Red Flag)
* **Legitimate Process:** `GetMappedFileName` returns `C:\Windows\System32\svchost.exe`. We know the code in RAM is the exact code sitting on the hard drive. It is safe.
* **Hollowed Process:** `GetMappedFileName` returns **NULL (Empty)**. Furthermore, the memory type is listed as `MEM_PRIVATE`. This means there is active, runnable code executing in RAM that came from nowhere. 

If we find `PAGE_EXECUTE_READWRITE` memory that is `MEM_PRIVATE` (Unbacked) at the base address of a process, we have 100% mathematically proven that the process has been Hollowed Out or injected with malicious code. We immediately terminate the process!
