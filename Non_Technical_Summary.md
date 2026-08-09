# ZeroTrustMonitor: Simple Summary for Users

Welcome to the ZeroTrustMonitor! This document explains how we built this tool to keep your computer completely safe, written in plain English.

## 1. Smart and Safe Monitoring
**What we did:** We designed the tool so it won't crash your computer. 
**Why it matters:** Advanced security tools (like the ones used in huge corporations) often inject tracking code into every single program running on your computer. If that code has a tiny bug, it crashes the whole system (this is exactly what caused the massive CrowdStrike global outage!). Instead, our tool lets you pick exactly which app you want to monitor, keeping the rest of your computer running safely and at full speed.

## 2. Catching Sneaky Hackers (Digital Fingerprints)
**What we did:** We upgraded the file scanner to use "Digital Fingerprints" (SHA-256 Hashes) instead of looking at the file's "Last Modified" date.
**Why it matters:** Hackers are tricky. If they alter a file on your computer, they can change the file's date back to 2023 to make it look like they were never there (a trick called "Timestomping"). Our tool ignores the fake dates and scans the actual DNA of the file to guarantee it hasn't been tampered with.

## 3. Defeating "Noise" Attacks
**What we did:** We gave our real-time monitor a massive memory upgrade.
**Why it matters:** If a hacker knows they are being watched, they might try to drop 10,000 dummy files onto your computer in a single second to overwhelm the security sensor and blind it while they do their real attack. We upgraded the memory buffer to its absolute maximum limit so it never gets overwhelmed by these "Noise Attacks."

## 4. Cutting-Edge Code (Native AOT)
**What we did:** We built the security sensor using modern "Native AOT" technology and dropped support for ancient 32-bit apps.
**Why it matters:** We stripped out all the bulky framework code and compiled our sensor into pure, raw machine language. This makes it lightning-fast, invisible, and able to slide seamlessly into modern applications without them even realizing they are being monitored.

## 5. Seeing the Invisible (ETW)
**What we did:** We hooked our tool directly into the deep Windows Kernel using ETW (Event Tracing for Windows).
**Why it matters:** Normally, Windows can tell you *that* a file was changed, but it won't tell you *who* changed it. By hooking into the deep Windows nervous system, our tool acts like a security camera—it catches the exact ID of the rogue script that changed your files, allowing the tool to instantly terminate the hacker's program.

## 6. Blocking "Body Snatchers"
**What we did:** We built a Global Process Monitor that enforces family trees.
**Why it matters:** Hackers love to launch official Windows programs (like `svchost.exe`, which normally does Windows Updates) and "hollow" them out, stuffing their own malicious code inside. To your firewall, it looks like a safe Windows Update. Our tool looks at the family tree. Since we know official Windows Updates are only ever launched by the main Windows Service Manager, if our tool sees your web browser or a script try to launch `svchost.exe`, it knows it's a Body Snatcher attack and instantly destroys it.
