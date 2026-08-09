# Windows 11 Security Terminology (Explained Simply)

This document translates the highly technical security terminology used by Microsoft into clear, understandable concepts while retaining the actual names so you can learn them.

---

## 1. Controlled Folder Access (CFA)
**What it sounds like:** A file system access control list.
**What it actually is:** Ransomware Protection.

**Simple Explanation:** 
Imagine your "Documents" folder is a bank vault. By default, Windows lets *any* program you run walk into the vault and change the files. If you run malware, it walks in, encrypts all your files, and demands money (Ransomware). 

When you turn on **Controlled Folder Access**, Windows posts an armed guard at the vault door. The guard has a strict VIP list of trusted, famous applications (like Microsoft Word or Adobe Photoshop). If an unknown program or rogue script tries to enter the vault to modify a file, the guard blocks them instantly and sends you a notification.

*(In the context of our Electron app: We add `C:\Program Files\` to the vault. If a worm tries to drop a malicious DLL next to your Reolink app, the guard blocks the worm).*

---

## 2. Arbitrary Code Guard (ACG)
**What it sounds like:** A prevention mechanism for unsanctioned code execution.
**What it actually is:** The "No new code allowed" rule.

**Simple Explanation:**
When a normal program runs, it loads its code from the hard drive into memory, and then runs it. 
When *malware* attacks a program, it sneaks into the program's memory, creates a brand new blank space, writes its own malicious code into that space, and tricks the computer into running it. 

**ACG** is a rule enforced by the Windows Kernel that says: *"Once a program starts, it is absolutely forbidden from creating any new executable code in memory."* It completely stops the malware's trick. However, it also breaks web browsers, because browsers *legitimately* use this trick (JIT Compilation) to make complex websites run fast.

---

## 3. Just-In-Time (JIT) Compilation
**What it sounds like:** A runtime compiler strategy.
**What it actually is:** On-the-fly translation.

**Simple Explanation:**
Imagine a web browser as an English speaker, and JavaScript (the code that runs websites) as a book written in Spanish.
* **Interpretation (Slow):** The browser reads the Spanish book one word at a time, looking up every word in a dictionary before speaking it.
* **JIT Compilation (Fast):** The browser employs a translator who instantly reads a whole page, translates it into perfect English (Machine Code), writes it down on a new piece of paper (Memory), and hands it to the browser to read instantly. 

Because JIT requires writing new things down on a blank piece of memory, it looks exactly like what malware does, which is why ACG blocks it.

---

## 4. Smart App Control (SAC)
**What it sounds like:** An intelligent application manager.
**What it actually is:** The Ultimate Bouncer (Zero-Trust execution).

**Simple Explanation:**
Historically, Windows would let you download and run almost any `.exe` file you found on the internet. It relied on Antivirus to scan the file *after* you clicked it to see if it was bad.

**Smart App Control** flips this upside down. It assumes *everything* is bad unless proven otherwise. Before any program or script is allowed to start, SAC checks its ID (its Digital Signature). If the program isn't signed by a reputable company, or if Microsoft's AI cloud hasn't seen millions of other people using it safely, SAC blocks it from ever opening. 

---

## 5. AppContainer (Low Integrity)
**What it sounds like:** A containerized application deployment.
**What it actually is:** A padded room with no doors.

**Simple Explanation:**
If a malicious website manages to hack your Google Chrome browser, why doesn't it immediately delete all your Windows files? Because Chrome's website-rendering engine is locked inside an **AppContainer**. 

An AppContainer is a padded room. The program inside it is running, but it has no permission to see your files, no permission to see other apps, and no permission to change Windows settings. The malware is trapped in the room. To hurt your computer, the hacker must find a second, incredibly rare "Sandbox Escape" vulnerability to break the door down.
