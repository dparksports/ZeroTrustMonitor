# Detecting Windows Sound API Changes

The user asked: *"How does this CLI detect any changes to the Windows sound API by any app or script?"*

There are two different ways to detect changes to the Windows Sound API. Our project has explored both approaches.

## 1. The Polling/Snapshot Approach (Currently in the CLI)
Right now, the `ZeroTrustMonitor` CLI uses a safe, read-only approach using a library called **NAudio**.

**How it works:**
1. The CLI asks Windows for the `MMDeviceEnumerator` (Multimedia Device Enumerator).
2. It loops through all the active "Audio Sessions" (every app currently making or receiving sound).
3. It checks the `SimpleAudioVolume.Mute` property for each session.
4. **The Result:** It can tell you *if* your application (like Reolink) is currently muted. 

**The Limitation:** It only tells you the current state. If a rogue PowerShell script muted your app 5 minutes ago, NAudio can tell you it is muted, but it cannot tell you *who* did it or *when* it happened.

## 2. The EDR Hooking Approach (The "Red Handed" Method)
To catch a rogue script or app *in the act* of changing the volume, we must use the **EDR (Endpoint Detection and Response) Hooking** approach that we documented earlier (using the `MinHookPayload.cs` and `CustomApiMonitor.cs` concepts).

**How it works:**
1. **Injection:** The security tool injects a small piece of code (a hook) directly into the memory of the target application (or hooks the OS audio service).
2. **Interception:** It rewrites the first few bytes of the Windows API function `IAudioEndpointVolume::SetMute`.
3. **The Trap:** When a rogue script or process tries to call `SetMute` to silence your app, the CPU is redirected to *our* custom code first.
4. **Catching them Red-Handed:** Because our custom code is executed the exact millisecond the attack happens, we can examine the "Call Stack". The call stack tells us the exact Process ID, the specific `.dll` file, and the line of assembly code that tried to mute the audio. We can then instantly block the request and log the attacker.

**Summary:** The NAudio approach is good for checking the *current status*. The Hooking approach is required to *catch the attacker* the moment they try to interact with the API.
