# Windows Security Model and Electron App Tainting

## Why does Windows 11 allow process injection?

It may seem like a glaring security flaw that Windows allows one process to inject code or threads into another. However, this is actually a fundamental design choice based on the Windows security model and application ecosystem.

### 1. The User Boundary (Mandatory Integrity Control)
Windows security is primarily based on the **User Account**, not the individual application. 
* If you (User A) run an Electron app, and you also run a rogue script, both execute at the **Medium Integrity Level** under your user account.
* Windows assumes that if a user executes a program, that program is acting on behalf of the user and has the right to interact with the user's other programs.
* **Protection exists:** A program running at a "Low" integrity level (like the sandboxed tab of a web browser) *cannot* inject into a "Medium" integrity level app. Similarly, a Medium app cannot inject into an "High" (Administrator) or "System" process. However, if malware successfully runs as your user, it has full rights to your other user-level apps.

### 2. Legitimate Use Cases
Process injection and memory manipulation are heavily relied upon by legitimate software. If Windows outright blocked the `CreateRemoteThread` or `WriteProcessMemory` APIs, the following software would break:
* **Antivirus & EDRs:** They must inject into processes to monitor behavior and scan for malware in memory.
* **Overlays:** Discord, Steam, and Xbox Game Bar inject DLLs into games and apps to render their UI on top.
* **Screen Recorders:** OBS Studio injects into games for high-performance capture.
* **Debuggers & Profilers:** Visual Studio and developers need these APIs to pause apps, read memory, and fix bugs.

## How to detect if your Electron app is "tainted" after install

If malware cannot achieve real-time memory injection (perhaps because an Antivirus blocks it), it will try to achieve **persistence** by "tainting" the application files on disk. Electron apps are particularly vulnerable to this because they bundle JavaScript, which is easy to read and modify.

Here is how you can verify if your Electron app has been compromised:

### 1. The ASAR Archive Check (The most common attack vector)
Electron apps package their core JavaScript source code into an archive file, usually located at `resources\app.asar`.
* Malware will often unpack the ASAR, insert malicious JavaScript, and repack it.
* **How to check:** Compare the SHA-256 hash of the `app.asar` file on your disk with the hash of a cleanly downloaded version from the vendor. If they don't match, the app's source code has been altered.

### 2. Digital Signature (Authenticode) Verification
Legitimate Electron `.exe` and `.dll` files are cryptographically signed by the developer.
* **How to check:** Right-click the application `.exe`, go to **Properties**, and check the **Digital Signatures** tab.
* Select the signature and click **Details**. It should say "This digital signature is OK."
* If malware modified the actual `.exe` file to inject its payload, the signature will be broken or missing entirely.

### 3. Check for Malicious `NODE_OPTIONS`
Electron runs on Node.js. Node.js supports an environment variable called `NODE_OPTIONS` that can force it to load a specific script before anything else.
* Malware can set a system-wide environment variable: `NODE_OPTIONS=--require C:\Users\Public\malware.js`.
* **How to check:** Open a command prompt and type `echo %NODE_OPTIONS%`. If it points to a script you don't recognize, every Electron and Node app on your system is being injected at startup.

### 4. Inspect Shortcut Hijacking
Malware often avoids touching the app files completely and instead taints how the app is launched.
* **How to check:** Right-click the shortcut you use to launch the app (on the Desktop or Start Menu) and check the **Target** field.
* A clean target looks like: `"C:\Program Files\App\app.exe"`
* A tainted target might look like: `"C:\Program Files\App\app.exe" --inspect=127.0.0.1:9229` (opening a debug port for the malware to control it) or it might point to a completely different script that launches the app silently.

### 5. Review the "resources" folder for rogue DLLs
Sometimes malware drops a malicious DLL into the Electron app's installation folder (e.g., `C:\Program Files\App\`). 
* When the app starts, Windows searches the app's local directory for dependencies first.
* If the malware names its file `version.dll` or `user32.dll` and places it next to the `.exe`, the Electron app will accidentally load the malicious DLL instead of the real Windows one. This is known as **DLL Hijacking** or **DLL Search Order Hijacking**.
