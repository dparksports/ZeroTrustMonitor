# Activated vs. Armed in Cybersecurity

In the physical security and cybersecurity worlds, the terms **"Activated"** (or Enabled) and **"Armed"** are often used together, but they mean two distinct things regarding the state of a defense mechanism.

## 1. Activated (or Enabled)
**Definition:** The system is turned on, powered up, and capable of functioning, but it might not be actively looking for a threat or ready to fire a response.

* **Physical Example:** You plug your home security system into the wall and connect it to Wi-Fi. The cameras are on, the sensors have power, and the control panel is lit up. The system is *Activated*.
* **Cybersecurity Example (Exploit Protection):** You open Windows Settings and toggle "Arbitrary Code Guard" to ON. The Windows Kernel loads the necessary drivers and rule sets into memory. The feature is *Activated* (Enabled) on the operating system.

## 2. Armed
**Definition:** The system is actively monitoring its environment, processing events, and is ready to immediately deploy its defensive countermeasures if a specific trigger condition is met.

* **Physical Example:** You type your PIN code into the home security panel before leaving the house. Now, if a door opens, the siren will blast and the police will be called. The system is *Armed*.
* **Cybersecurity Example (Our CLI Tool):** Our `ZeroTrustMonitor` CLI is launched. It injects its `FileSystemWatcher` hooks directly into the OS event stream for the `C:\Program Files\Reolink` directory. It is now sitting in a 0% CPU sleep state, *waiting* for a file modification event to trigger its Tamper Alert. The monitor is *Armed*.

## Summary
* **Activated:** The software is installed, configured, and turned on.
* **Armed:** The software is actively "watching the door" and ready to trigger an alarm or block an attack the moment it happens.

In our CLI, when we say the security protocols are **Activated and Armed**, we mean Windows Exploit Protection has been turned on (Activated), and our real-time tamper monitor is actively hooked into the OS and waiting for a threat (Armed).
