# Browser Sandboxing on Windows 11

Just like on macOS, modern web browsers on Windows (Chrome, Edge, Firefox) **do** run inside a heavily restricted sandbox. In fact, the Windows security model provides multiple layers of sandboxing to ensure that if a malicious website hacks the browser's rendering engine, it cannot escape and hack the underlying operating system.

Here is how browser sandboxing works on Windows and how you can enforce the strictest levels of isolation.

## Layer 1: The Browser's Internal Sandbox (Multi-Process Architecture)
Modern browsers like Google Chrome and Microsoft Edge use a multi-process architecture.
* **The Browser Process:** Runs at a normal "Medium" integrity level. It handles the UI, tabs, and file downloads.
* **The Renderer Processes:** Every tab you open creates a separate Renderer process. The Renderer's job is to parse the HTML and execute the JavaScript of the website you are visiting.

**The Isolation:** On Windows, the Renderer process is launched with a **"Low" or "Untrusted" Integrity Level** token. 
* If a malicious website exploits a 0-day vulnerability in the V8 JavaScript engine and completely takes over the Renderer process, it is trapped. 
* Because it is running at a Low Integrity Level, Windows blocks it from reading or writing to your Documents, modifying the registry, or injecting code into Medium/High integrity applications (like your Electron apps or system services).
* To hack the OS, the malware must possess a *second* 0-day vulnerability: a "Sandbox Escape" or "Privilege Escalation" exploit to break out of the Low Integrity process.

## Layer 2: Windows AppContainer Isolation
Windows provides an OS-level sandboxing framework called **AppContainer** (originally built for UWP/Store apps). 
* AppContainers provide fine-grained, capability-based security. An app inside an AppContainer cannot access the network, webcam, or file system unless explicitly granted that specific "capability" by the OS.
* Microsoft Edge and Google Chrome leverage AppContainers for their Renderer processes, meaning the tab isn't just low integrity; it is explicitly blocked by the Windows Kernel from touching unauthorized OS resources.

## Layer 3: The Ultimate Sandbox - Microsoft Defender Application Guard (Hardware Virtualization)
If you want a sandbox so secure that even a successful Sandbox Escape exploit cannot harm your Windows OS, you can use **Microsoft Defender Application Guard (WDAG)**. This feature is unique to Windows 11 Pro and Enterprise.

Instead of just using software-level integrity tokens, WDAG uses **Hyper-V (Hardware Virtualization)**.
* When you navigate to an untrusted website in Microsoft Edge, WDAG spins up a lightweight, invisible **Micro-Virtual Machine (Micro-VM)** at the hardware level.
* The website runs entirely inside this isolated VM.
* **The Result:** If a nation-state hacker uses a flawless 0-day chain to completely compromise the browser and achieve SYSTEM level privileges... they only achieve SYSTEM privileges *inside the disposable Micro-VM*. 
* They have absolutely zero access to your real hard drive, your real memory, or your real OS. When you close the tab, the Micro-VM is instantly destroyed, and the malware is wiped from existence.

### How to Enable Application Guard (Hardware Sandbox)
If you are on Windows 11 Pro or Enterprise, you can enable this ultimate sandbox:
1. Press the Windows Key and type **Turn Windows features on or off**.
2. Scroll down and check the box for **Microsoft Defender Application Guard**.
3. Click OK and restart your computer.
4. Open Microsoft Edge, click the three dots (Menu), and select **New Application Guard window**. 
5. Any website you browse in that window is executing inside a hardware-isolated Hyper-V Micro-VM.

## Summary
While you cannot apply Arbitrary Code Guard (ACG) to browsers because it breaks their JavaScript engines, Windows solves the problem by putting the entire JavaScript engine into a tightly sealed box (Low Integrity / AppContainer). For high-risk browsing, hardware virtualization (WDAG) provides an impenetrable wall between the bad site and your actual computer.
