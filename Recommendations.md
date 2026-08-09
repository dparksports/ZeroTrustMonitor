# Audio Troubleshooter Recommendations

This document outlines the steps to detect and troubleshoot an issue where a worm or malicious process mutes or disables sound for specific Electron applications on Windows 11.

## Diagnostic Steps

### 1. Observe the Volume Mixer Behavior
- Open the affected Electron app and attempt to play audio.
- Open the Windows Volume Mixer (right-click the speaker icon in the system tray).
- Observe the volume slider for the specific app. If the slider moves down automatically or the mute icon toggles on its own, another process is actively calling the Core Audio API to manipulate it.

### 2. Trace API Calls with Process Monitor (ProcMon)
- Use **Sysinternals Process Monitor** (run as Administrator).
- Add a filter: `Path` contains `Audio` -> `Include`.
- Monitor for registry or file activity related to audio sessions, such as modifications to `HKCU\Software\Microsoft\Internet Explorer\LowRegistry\Audio\PolicyConfig\PropertyStore`.
- Watch for suspicious processes writing to audio-related registry keys or loading audio DLLs (`audioses.dll`, `mmdevapi.dll`).

### 3. Check for DLL Injection with Process Explorer
- Use **Sysinternals Process Explorer** (run as Administrator).
- Locate the main process of the Electron app.
- Inspect the **Threads** or **Memory** tab for suspicious threads starting from unknown memory addresses.
- View loaded DLLs (Lower Pane View -> DLLs) and look for unsigned DLLs or those located outside of standard Windows/App directories.

### 4. Monitor the Windows Audio Service
- Open **Event Viewer** (`eventvwr.msc`).
- Check `Windows Logs` -> `Application` and `System` for warnings or errors related to `Audiosrv` (Windows Audio) or crashes of the specific Electron application.

### 5. Check Audio Enhancements
- Navigate to **Settings > System > Sound**.
- Open the properties of your output device and turn off **Audio enhancements**.
- If this resolves the issue, a third-party software (potentially malicious) has installed a filter driver that drops the audio stream.

## Automated Troubleshooting CLI

The accompanying `.cs` and `.csproj` files contain a .NET 10 CLI application that automates the process of checking active audio sessions for unexpected mute/volume states and scanning process modules for potentially injected DLLs. You can build it by running `dotnet build` in this directory.
