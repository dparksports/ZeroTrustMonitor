# EDR Hooking: Targeted vs. Global Performance

The user asked: *"Would hooking all apps cause a performance hit?"*

**Yes, injecting an EDR Hook into every single running application has a significant performance and stability impact.** Here is a breakdown of why, and why allowing the user to choose is the best design.

## 1. The Performance Hit of Global Hooking

When you type `ALL` to deploy the EDR Hook globally, the CLI must do the following:
1. **CPU Overhead:** It loops through all ~150 to 300 active processes running on Windows.
2. **Context Switching:** For every single process, it must ask the Windows Kernel for permission (`OpenProcess`), halt the process temporarily, carve out memory, write the payload, and force a new thread (`CreateRemoteThread`). 
3. **Memory Overhead:** Our `AudioTrap.dll` is injected into 150 separate memory spaces. If the DLL is 2MB, that is an immediate 300MB of RAM consumed just to hold the trap.
4. **Execution Overhead:** Every time *any* app on the computer tries to play a sound or check the volume, the CPU must first execute our custom Detour code before allowing the sound to play. 

## 2. The Stability Risk (The "Blue Screen" Danger)

Global injection is incredibly dangerous if the injected code is not absolutely perfect.
* If our `AudioTrap.dll` has a tiny memory leak or a null-reference bug, and we inject it into 150 processes, **we will crash all 150 processes simultaneously.** 
* This is exactly how the global CrowdStrike outage happened. A bug in a globally injected sensor caused the entire Windows OS to crash.

## 3. The Best UX Design: The Hybrid Approach

Because of the performance and stability risks, the most professional way to design the CLI is a **Hybrid Approach**:
* By default, the CLI starts with no arguments and scans the system securely.
* When the user selects the EDR Hooking menu option, the CLI **prompts them**: *"Enter the name of the app to hook, or type ALL."*
* This allows advanced users to monitor the entire system if they are willing to accept the performance hit, but allows standard users to just type `reolink` and surgically monitor the one app they are suspicious of with **0% performance impact** on the rest of the OS.
