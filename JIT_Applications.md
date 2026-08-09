# Applications that Rely on JIT (Just-In-Time) Compilation

Just-In-Time (JIT) compilation is a technique used to improve the performance of software. Instead of interpreting code line-by-line, a JIT compiler translates the code (like JavaScript, C#, or Java) into native machine code (assembly) on the fly, right before it is executed. To do this, it must allocate memory, write the new machine code into it, and mark that memory as executable.

Because this behavior looks identical to how malware injects shellcode, **you cannot apply strict memory mitigations like Arbitrary Code Guard (ACG) to JIT-based applications**, or they will instantly crash.

Here is a list of common application types and specific apps that rely on JIT compilation:

## 1. Web Browsers (JavaScript Engines)
All modern web browsers use complex JIT compilers to make complex websites and web apps run fast.
* **Google Chrome** (uses the V8 engine)
* **Microsoft Edge** (uses the V8 engine)
* **Mozilla Firefox** (uses the SpiderMonkey engine)
* **Safari/WebKit** (uses JavaScriptCore)

## 2. Electron & Chromium-Embedded Applications
Electron is a framework that bundles the Chromium browser (and its V8 JIT engine) and Node.js to create desktop applications using web technologies.
* **Communication:** Discord, Slack, Microsoft Teams, WhatsApp, Skype
* **Development:** Visual Studio Code, Atom, GitHub Desktop
* **Media & Utilities:** Spotify, Twitch, Figma, Notion, Bitwarden
* **Custom Apps:** The official Reolink desktop client.

## 3. Managed Runtimes (.NET and Java)
Enterprise software and many desktop applications are written in languages that compile to an intermediate language, which is then JIT-compiled on the user's machine.
* **Java Applications:** Any application running on the Java Virtual Machine (JVM), such as Minecraft (Java Edition), JetBrains IDEs (IntelliJ IDEA, WebStorm), and many enterprise banking tools.
* **.NET Framework / .NET Core Applications:** Most traditional C# or VB.NET desktop applications (WPF, Windows Forms) rely on the .NET JIT compiler (RyuJIT).
  - *Exception:* Modern .NET apps can be published using **Native AOT** (Ahead-Of-Time). AOT apps are pre-compiled entirely into native machine code on the developer's machine and *do not* use JIT. You *can* safely apply ACG to Native AOT apps!

## 4. Scripting Language Engines (Sometimes)
Some modern interpreters for scripting languages utilize JIT to boost performance.
* **Node.js:** Powered by the V8 engine (JIT heavily used).
* **Python:** Standard CPython does not use JIT (it interprets), but alternative runtimes like PyPy do.
* **Lua:** Standard Lua interprets, but LuaJIT is specifically built around a JIT compiler.

## Summary: What CAN you protect with ACG?
If you want to enable Arbitrary Code Guard to lock down memory, you must apply it only to applications that are **Ahead-Of-Time (AOT) compiled**. This includes:
* Traditional C and C++ software.
* Rust or Go applications.
* C# applications explicitly compiled with Native AOT.
* Built-in Windows system utilities (Notepad, Calculator, etc.).
