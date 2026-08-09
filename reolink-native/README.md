# Reolink Native Open Source Project

This project aims to build an open-source, highly optimized native Windows client for Reolink cameras, serving as a replacement for the resource-heavy Electron-based official application.

## Why Native C# over Electron?
Electron applications bundle an entire Chromium web browser and Node.js runtime. For video streaming, this results in:
* High Memory Usage (often 500MB+ for a single stream).
* High CPU Usage, as it struggles to efficiently pass 4K streams to the GPU for hardware decoding.
* Vulnerability to script-based process injection and tampering, as strict memory mitigations (like Arbitrary Code Guard) crash the underlying V8 JavaScript engine.

A Native C# .NET application directly interfaces with the Windows OS, resolving all these issues.

## Recommended Architecture

### 1. UI Framework: WinUI 3 (Windows App SDK) or WPF
To achieve a modern Windows 11 design with fluid animations and optimal performance:
* **WinUI 3**: The latest native UI framework. It uses XAML for layout and C# for logic, offering the best integration with Windows 11 hardware acceleration and Mica design materials.
* **WPF**: A highly stable and feature-rich alternative.

### 2. Video Streaming & Decoding: LibVLCSharp
Reolink streams video using RTSP or RTMP (H.264/HEVC). 
* **Engine**: Use **LibVLCSharp**, the official C# wrapper for the VLC media player. 
* **Hardware Acceleration**: LibVLC natively supports DirectX Video Acceleration (DXVA), offloading the 4K decoding entirely to the GPU, keeping CPU usage near zero.

### 3. Communication Protocol: Reolink CGI API (HTTP)
Camera control is handled via the Reolink local HTTP API (CGI API).
* Use the built-in `HttpClient` in C# to issue GET/POST requests.
* **Features**: Authentication, PTZ (Pan-Tilt-Zoom) controls, and configuration changes (resolution, frame rate) via JSON.

### 4. Audio (Two-Way Talk): NAudio
For microphone access:
* Use **NAudio** to interface with Windows Core Audio (WasapiCapture). This captures the raw microphone buffer to be transmitted to the camera via the HTTP API.

### 5. Enhanced Security
A compiled .NET native executable can be locked down:
* **Arbitrary Code Guard (ACG)** can be enabled safely via Windows Exploit Protection, severely limiting the ability of malware to inject shellcode.
* The executable can be signed and explicitly trusted via Smart App Control (SAC) or Windows Defender Application Control (WDAC).

## Getting Started

To initialize the codebase for this project:

```bash
# Create a new WinUI 3 project
dotnet new winui -n NativeReolinkClient

# Add required dependencies
cd NativeReolinkClient
dotnet add package LibVLCSharp.WinUI
dotnet add package NAudio
```
