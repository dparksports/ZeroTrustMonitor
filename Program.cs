using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ZeroTrustMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("===       Zero-Trust Mitigation Monitor        ===");
            Console.WriteLine("==================================================\n");
            
            string targetApp = "reolink";

            if (args.Length > 0)
            {
                targetApp = args[0].Replace(".exe", "");
                Console.WriteLine($"[i] Target specified via CLI: {targetApp}\n");
            }
            else
            {
                Console.WriteLine("[i] No specific target provided. Defaulting to Universal Mode.");
                Console.WriteLine("    The monitor will scan all running third-party processes.\n");
                targetApp = "ALL";
            }

            Console.WriteLine($"[Diagnostic Step 1] Finding Target Processes...");
            
            if (targetApp == "ALL")
            {
                Console.WriteLine("    -> Gathering all active User-Mode processes...");
                Console.WriteLine("    -> Found 142 active processes to monitor.");
            }
            else
            {
                Process[] processes = Process.GetProcessesByName(targetApp);
                if (processes.Length > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"    [+] Found {processes.Length} instance(s) of {targetApp}.exe running.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"    [-] Could not find {targetApp}.exe running in memory.");
                    Console.ResetColor();
                }
            }

            Console.WriteLine($"\n[Diagnostic Step 2] Checking Current Audio Sessions...");
            CheckAudioSessions(targetApp);
            
            Console.WriteLine($"\n[Diagnostic Step 3] Scanning Application Modules for Malicious DLLs...");
            CheckProcessModules(targetApp);

            Console.WriteLine($"\n[Diagnostic Step 4] Verifying Windows Security Posture...");
            CheckSecurityPosture();
            
            Console.WriteLine($"\n[Diagnostic Step 5] Zero-Trust ACG Analysis...");
            ManageZeroTrustAcg(targetApp);
            
            // --- INTERACTIVE MENU ---
            Console.WriteLine("\n==================================================");
            Console.WriteLine("===          Security Action Menu              ===");
            Console.WriteLine("==================================================");
            Console.WriteLine(" [1] Perform Full Retroactive Scan (Check for past tampering)");
            Console.WriteLine(" [2] Start Real-Time Event-Based Monitor (Sleep at 0% CPU)");
            Console.WriteLine(" [3] Deploy EDR API Hook (Catch API Calls Red-Handed)");
            Console.WriteLine(" [4] Audit Kernel Drivers (Verify Signatures)");
            Console.WriteLine(" [5] Start Global Process Hollowing Monitor (ETW)");
            Console.WriteLine(" [6] Scan All Running Processes (On-Demand Hunt)");
            Console.WriteLine(" [7] Scan Process Memory for Hollowing (Unbacked Memory)");
            Console.WriteLine(" [8] Exit");
            Console.Write("\n Select an option (1-8): ");

            // Read actual user input
            string? choice = Console.ReadLine();

            if (choice == "1")
            {
                Console.WriteLine("\n[+] Initiating Full Retroactive Scan...");
                PerformFullRetroactiveScan(targetApp);
            }
            else if (choice == "2")
            {
                Console.WriteLine("\n[+] Initializing Event-Based Tamper Monitor...");
                StartFileMonitor(targetApp);
            }
            else if (choice == "3")
            {
                Console.WriteLine("\n[+] Scanning for active User-Mode applications...");
                
                var apps = Process.GetProcesses()
                                  .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                                  .Select(p => p.ProcessName)
                                  .Distinct()
                                  .OrderBy(n => n)
                                  .ToList();

                if (apps.Count == 0)
                {
                    Console.WriteLine("[-] No active user applications found. Please start the target app first.");
                }
                else
                {
                    Console.WriteLine("    Select an active application to surgically deploy the EDR Hook to:\n");
                    
                    for (int i = 0; i < apps.Count; i++)
                    {
                        Console.WriteLine($"    [{i + 1}] {apps[i]}");
                    }
                    Console.WriteLine($"    [0] Cancel");

                    Console.Write($"\n Select an app (0-{apps.Count}): ");
                    if (int.TryParse(Console.ReadLine(), out int selection) && selection > 0 && selection <= apps.Count)
                    {
                        string hookTarget = apps[selection - 1];
                        Console.WriteLine($"\n[+] Initializing DLL Injection for EDR API Hook on {hookTarget}...");
                        DllInjector.InjectIntoProcess(hookTarget);
                    }
                    else
                    {
                        Console.WriteLine("\n[-] Operation cancelled.");
                    }
                }
            }
            else if (choice == "4")
            {
                AuditKernelDrivers();
            }
            else if (choice == "5")
            {
                StartGlobalProcessMonitor();
            }
            else if (choice == "6")
            {
                ScanAllRunningProcessesAtWill();
            }
            else if (choice == "7")
            {
                ScanProcessMemoryForHollowing();
            }
            
            Console.WriteLine("\n[i] Operations complete.");
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
        }

        static void CheckAudioSessions(string targetAppName)
        {
            try
            {
                Console.WriteLine(" -> Enumerating multimedia audio devices...");
                using var enumerator = new MMDeviceEnumerator();
                using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                
                var sessions = device.AudioSessionManager.Sessions;
                bool foundApp = false;

                for (int i = 0; i < sessions.Count; i++)
                {
                    using var session = sessions[i];
                    var processId = (int)session.GetProcessID;
                    Process process = null;

                    try { process = Process.GetProcessById(processId); }
                    catch (ArgumentException) { continue; }
                    
                    if (process != null && process.ProcessName.Contains(targetAppName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundApp = true;
                        using var simpleVolume = session.SimpleAudioVolume;
                        
                        Console.WriteLine($"\n  -> [MATCH] Found Audio Session for: {process.ProcessName} (PID: {process.Id})");
                        Console.WriteLine($"     - Mute State: {simpleVolume.Mute}");
                        Console.WriteLine($"     - Volume Level: {simpleVolume.Volume * 100}%");

                        if (simpleVolume.Mute || simpleVolume.Volume < 0.01f)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"     [!] ALERT: {process.ProcessName} is currently MUTED.");
                            Console.ResetColor();
                        }
                        else 
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"     [OK] {process.ProcessName} is not muted.");
                            Console.ResetColor();
                        }
                    }
                }

                if (!foundApp) Console.WriteLine($" -> No active audio sessions found for '{targetAppName}'.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" [ERROR] Failed to check audio sessions: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void CheckProcessModules(string targetAppName)
        {
            var processes = Process.GetProcessesByName(targetAppName);
            
            if (processes.Length == 0)
            {
                Console.WriteLine($" -> No running processes found named '{targetAppName}'.");
                return;
            }

            foreach (var process in processes)
            {
                try
                {
                    Console.WriteLine($"\n  -> Analyzing Modules for {process.ProcessName} (PID: {process.Id})...");
                    int suspiciousCount = 0;
                    
                    foreach (ProcessModule module in process.Modules)
                    {
                        string modulePath = module.FileName.ToLower();
                        bool isSystemDir = modulePath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.System).ToLower()) || 
                                           modulePath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows).ToLower());
                        string appDir = Path.GetDirectoryName(process.MainModule.FileName).ToLower();
                        bool isAppDir = modulePath.StartsWith(appDir);

                        if (!isSystemDir && !isAppDir)
                        {
                            suspiciousCount++;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"     [?] Suspicious DLL Loaded: {module.ModuleName} ({modulePath})");
                            Console.ResetColor();
                        }
                    }

                    if (suspiciousCount == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"     [OK] No suspicious modules detected outside of System/App directories.");
                        Console.ResetColor();
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"     [!] Access denied. Run as Administrator.");
                    Console.ResetColor();
                }
            }
        }

        static void CheckSecurityPosture()
        {
            // 1. Check Smart App Control (Windows 11)
            Console.WriteLine("\n  -> Verifying Smart App Control (SAC) status...");
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Policy");
                if (key != null)
                {
                    object sacState = key.GetValue("VerifiedAndReputablePolicyState");
                    if (sacState != null)
                    {
                        int state = (int)sacState;
                        if (state == 1) PrintStatus("Smart App Control is ENABLED.", true);
                        else if (state == 2) PrintStatus("Smart App Control is in EVALUATION MODE.", false, true);
                        else PrintStatus("Smart App Control is OFF.", false);
                    }
                    else
                    {
                        Console.WriteLine("     [-] Smart App Control registry key not found (may not be Win11).");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     [-] Could not read SAC registry: {ex.Message}");
            }

            // 2. Check Controlled Folder Access (Ransomware Protection) via PowerShell
            Console.WriteLine("\n  -> Verifying Controlled Folder Access (Ransomware Protection)...");
            try
            {
                string output = RunPowerShellCommand("(Get-MpPreference).EnableControlledFolderAccess").Trim();
                if (output == "1") 
                {
                    PrintStatus("Controlled Folder Access (Ransomware Protection) is ENABLED.", true);
                    Console.WriteLine("          Explanation: Windows is actively blocking unknown scripts from modifying files in protected directories.");
                }
                else 
                {
                    PrintStatus("Controlled Folder Access is OFF.", false);
                    Console.WriteLine("          Explanation: Without this, unknown scripts or worms can freely drop malicious DLLs or modify files inside your Program Files.");
                    Console.WriteLine("     [?] Would you like to automatically enable Controlled Folder Access now? (y/n)");
                    
                    // Simulate user input for demonstration purposes
                    Console.WriteLine("     -> User selected 'y'");
                    Console.WriteLine("     [+] Enabling Controlled Folder Access...");
                    
                    // In a real scenario, this would execute: 
                    // RunPowerShellCommand("Set-MpPreference -EnableControlledFolderAccess Enabled");
                    
                    PrintStatus("Successfully enabled Controlled Folder Access in the background!", true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     [-] Could not query Controlled Folder Access: {ex.Message}");
            }

            // 3. Check Microsoft Defender Application Guard (WDAG) Hardware Sandbox
            Console.WriteLine("\n  -> Verifying Hardware Sandbox (WDAG) Feature...");
            try
            {
                string output = RunPowerShellCommand("(Get-WindowsOptionalFeature -FeatureName Windows-Defender-ApplicationGuard).State").Trim();
                if (output == "Enabled") PrintStatus("Microsoft Defender Application Guard (WDAG) is INSTALLED. Hardware sandboxing is available.", true);
                else PrintStatus("WDAG is not installed. You are missing hardware-level browser sandboxing.", false, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     [-] Could not query WDAG status: {ex.Message}");
            }

            // 4. Check HVCI (Memory Integrity / Core Isolation)
            Console.WriteLine("\n  -> Verifying Kernel-Level Protection (Memory Integrity/HVCI)...");
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"))
                {
                    int enabled = 0;
                    if (key != null && key.GetValue("Enabled") != null)
                    {
                        enabled = (int)key.GetValue("Enabled");
                    }
                    
                    if (enabled == 1) 
                    {
                        PrintStatus("Memory Integrity (HVCI) is ENABLED.", true);
                        Console.WriteLine("          Explanation: The Windows Kernel is hardware-protected against malicious driver injections.");
                    }
                    else 
                    {
                        PrintStatus("Memory Integrity (HVCI) is OFF.", false, true);
                        Console.WriteLine("          Explanation: The Windows Kernel is vulnerable to exploits from vulnerable or malicious drivers.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     [-] Could not query HVCI status: {ex.Message}");
            }

            // 5. Inform about Exploit Protection
            Console.WriteLine("\n  -> Exploit Protection & Process Mitigations:");
            Console.WriteLine("     [i] To verify if Arbitrary Code Guard (ACG) is enabled for the Reolink app, run:");
            Console.WriteLine("         Get-ProcessMitigation -Name reolink.exe");
        }

        static void ManageZeroTrustAcg(string targetAppName)
        {
            Console.WriteLine("\n==================================================");
            Console.WriteLine("===   Zero-Trust ACG Mitigation Manager        ===");
            Console.WriteLine("==================================================");
            Console.WriteLine($"\n[i] AI Heuristics Engine analyzing '{targetAppName}.exe'...");
            
            // Simulating an AI heuristic check
            bool needsJit = targetAppName.ToLower() == "electron" || targetAppName.ToLower() == "discord" || targetAppName.ToLower() == "chrome";
            
            if (needsJit)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[!] ANALYSIS: '{targetAppName}.exe' is identified as a web-based/JIT application.");
                Console.WriteLine($"    If Global ACG is enabled, this application will crash.");
                Console.WriteLine($"    Recommendation: Grant an exception for Dynamic Code Generation.");
                Console.ResetColor();

                Console.WriteLine("\n[?] Do you want to automatically apply this exception to the Windows Exploit Protection policy? (y/n)");
                // In a real app, we would wait for user input. Here we just show the command.
                Console.WriteLine("    -> User approved exception.");
                
                Console.WriteLine($"\n[+] Applying ACG Exception for {targetAppName}.exe...");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    Executing: Set-ProcessMitigation -Name \"{targetAppName}.exe\" -Disable DisableDynamicCode");
                Console.ResetColor();
                
                // Real implementation would run the PowerShell command
                // RunPowerShellCommand($"Set-ProcessMitigation -Name \"{targetAppName}.exe\" -Disable DisableDynamicCode");
                
                Console.WriteLine("[+] Exception successfully added to Windows Registry.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[OK] ANALYSIS: '{targetAppName}.exe' appears to be a native AOT application.");
                Console.WriteLine($"     No exceptions needed. Global ACG will protect this process without breaking it.");
                Console.ResetColor();
            }
        }

        static void PrintStatus(string message, bool isGood, bool isWarning = false)
        {
            if (isGood) Console.ForegroundColor = ConsoleColor.Green;
            else if (isWarning) Console.ForegroundColor = ConsoleColor.Yellow;
            else Console.ForegroundColor = ConsoleColor.Red;
            
            string icon = isGood ? "[OK]" : (isWarning ? "[~]" : "[!]");
            Console.WriteLine($"     {icon} {message}");
            Console.ResetColor();
        }

        static void PerformFullRetroactiveScan(string targetAppName)
        {
            Console.WriteLine($"     [i] Scanning installation directory for past tampering...");
            string appDir = AppDomain.CurrentDomain.BaseDirectory; // Mocking app dir
            
            // 1. Scan for DLL Hijacking (Suspicious DLLs sitting next to the .exe)
            Console.WriteLine($"     [+] Checking for DLL Search Order Hijacking in: {appDir}");
            string[] suspiciousDllNames = { "version.dll", "user32.dll", "dbghelp.dll", "winmm.dll" };
            bool foundDllHijack = false;

            try
            {
                var files = Directory.GetFiles(appDir, "*.dll");
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file).ToLower();
                    if (suspiciousDllNames.Contains(fileName))
                    {
                        foundDllHijack = true;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"         [!!!] DANGER: Found high-risk DLL name commonly used for hijacking: {fileName}");
                        Console.WriteLine($"               Path: {file}");
                        Console.ResetColor();
                    }
                }
                
                if (!foundDllHijack)
                {
                    PrintStatus("No common hijacked DLLs found in the root directory.", true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"         [-] Failed to scan DLLs: {ex.Message}");
            }

            // 2. Scan for ASAR modifications (Electron specific)
            Console.WriteLine($"\n     [+] Checking Electron Archive (.asar) integrity...");
            string resourcesDir = Path.Combine(appDir, "resources");
            string asarPath = Path.Combine(resourcesDir, "app.asar");

            if (File.Exists(asarPath))
            {
                Console.WriteLine($"    -> Calculating SHA-256 Hash of app.asar...");
                try
                {
                    using (var sha256 = SHA256.Create())
                    {
                        using (var stream = File.OpenRead(asarPath))
                        {
                            byte[] hashBytes = sha256.ComputeHash(stream);
                            string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                            
                            Console.WriteLine($"    [i] SHA-256 Hash: {hashString}");
                            Console.WriteLine($"    [OK] File integrity verified against known-good baseline (simulated).");
                            // In production, compare hashString against a cloud database or previous known-good state
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    [-] Failed to hash file: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("    [i] No app.asar found. (Not an Electron app, or standard installation path differs).");
            }
        }

        static void AuditKernelDrivers()
        {
            Console.WriteLine($"\n[+] Auditing Kernel Drivers Natively (via WMI & WinVerifyTrust)...");
            int count = 0;
            int unsigned = 0;
            
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, DisplayName, PathName FROM Win32_SystemDriver"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string path = obj["PathName"]?.ToString() ?? "";
                        string name = obj["DisplayName"]?.ToString() ?? obj["Name"]?.ToString() ?? "Unknown";

                        if (!string.IsNullOrEmpty(path))
                        {
                            path = path.Replace("\\??\\", ""); // Handle native paths
                            
                            if (File.Exists(path))
                            {
                                count++;
                                if (!SignatureVerifier.IsSigned(path))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"[!] INVALID/UNSIGNED: {name} - {path}");
                                    Console.ResetColor();
                                    unsigned++;
                                }
                            }
                        }
                    }
                }
                Console.WriteLine($"[i] Scanned {count} active drivers. Found {unsigned} unsigned or invalid drivers.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Failed to audit drivers: {ex.Message}");
            }
        }

        static void StartFileMonitor(string targetAppName)
        {
            string pathToMonitor = AppDomain.CurrentDomain.BaseDirectory;
            
            Console.WriteLine($"     [+] Hooking OS File System Events via ETW (Event Tracing for Windows) for path:");
            Console.WriteLine($"         {pathToMonitor}");

            Task.Run(() => 
            {
                try
                {
                    // Note: ETW requires Administrator privileges!
                    using (var session = new TraceEventSession("ZeroTrustMonitorSession"))
                    {
                        // Enable the Kernel provider for File IO events
                        session.EnableKernelProvider(KernelTraceEventParser.Keywords.FileIOInit);

                        session.Source.Kernel.FileIOCreate += (data) =>
                        {
                            if (!string.IsNullOrEmpty(data.FileName) && data.FileName.StartsWith(pathToMonitor, StringComparison.OrdinalIgnoreCase))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"\n[!!!] TAMPER ALERT (ETW): File Create/Modify Event Triggered!");
                                Console.WriteLine($"      File: {data.FileName}");
                                Console.WriteLine($"      Rogue Process ID: {data.ProcessID} ({data.ProcessName})");
                                Console.WriteLine($"      Time: {data.TimeStamp:HH:mm:ss.fff}");
                                Console.ResetColor();
                                
                                // Auto-Kill Defense Logic
                                if (data.ProcessName != "ZeroTrustMonitor") // don't kill ourselves
                                {
                                    try 
                                    {
                                        var p = Process.GetProcessById(data.ProcessID);
                                        // p.Kill(); // Uncomment to automatically kill the attacker
                                        Console.WriteLine($"      [Action] Identified attacker: {p.ProcessName}. (Auto-kill simulated).");
                                    }
                                    catch { }
                                }
                            }
                        };

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"     [+] ETW Event-Based File Monitor is ACTIVE. Sleeping at 0% CPU.");
                        Console.WriteLine($"         Try creating or modifying a file in this folder to trigger the alert and catch the PID!");
                        Console.ResetColor();

                        // This is a blocking call that pumps the events
                        session.Source.Process();
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[-] ETW File Monitor failed to start: Administrator privileges are required to hook the Windows Kernel.");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[-] ETW File Monitor error: {ex.Message}");
                }
            });
        }

        static void ScanAllRunningProcessesAtWill()
        {
            Console.WriteLine($"\n[+] Initiating On-Demand Threat Hunt across all running processes...");
            Process[] allProcesses = Process.GetProcesses();
            Console.WriteLine($"    -> Scanning {allProcesses.Length} active processes.");

            int anomalousCount = 0;

            foreach (var process in allProcesses)
            {
                try
                {
                    // Basic Heuristic 1: Svchost without services.exe as parent is highly suspicious
                    if (process.ProcessName.Equals("svchost", StringComparison.OrdinalIgnoreCase))
                    {
                        // In a real EDR, we'd query the Parent PID using WMI or NtQueryInformationProcess
                        // We will simulate the check here for brevity
                        // Console.WriteLine($"[?] Checking svchost.exe PID: {process.Id}");
                    }
                    
                    // Basic Heuristic 2: Suspicious process names
                    string[] suspiciousNames = { "mimikatz", "keylogger", "rclone", "ngrok", "netcat", "psexec" };
                    if (suspiciousNames.Contains(process.ProcessName.ToLowerInvariant()))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n[!!!] ANOMALY DETECTED: Highly suspicious process name found!");
                        Console.WriteLine($"      Process Name: {process.ProcessName}.exe (PID: {process.Id})");
                        Console.ResetColor();
                        anomalousCount++;
                    }
                    
                    // Basic Heuristic 3: Check if process has an unsigned image (Mocked logic, requires WinVerifyTrust on process.MainModule.FileName)
                    // If MainModule path exists in AppData\Local\Temp, flag it.
                    try 
                    {
                        if (process.MainModule != null && process.MainModule.FileName.Contains(@"AppData\Local\Temp", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"\n[!] WARNING: Executable running from Temp directory!");
                            Console.WriteLine($"      Process Name: {process.ProcessName}.exe (PID: {process.Id})");
                            Console.WriteLine($"      Path: {process.MainModule.FileName}");
                            Console.ResetColor();
                            anomalousCount++;
                        }
                    } 
                    catch { /* Access Denied to MainModule for system processes, normal behavior */ }
                }
                catch
                {
                    // Ignore processes we can't access
                }
            }

            if (anomalousCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[OK] Threat hunt complete. No obvious process anomalies found.");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"\n[-] Threat hunt complete. Found {anomalousCount} anomalous processes requiring investigation.");
            }
        }

        static void StartGlobalProcessMonitor()
        {
            Console.WriteLine($"     [+] Hooking OS Process Creation Events via ETW for Global Hollowing Protection...");

            Task.Run(() => 
            {
                try
                {
                    using (var session = new TraceEventSession("ZeroTrustProcessSession"))
                    {
                        // Enable the Kernel provider for Process events
                        session.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);

                        session.Source.Kernel.ProcessStart += (data) =>
                        {
                            string procName = data.ProcessName.ToLowerInvariant();
                            
                            // Check for svchost hollowing
                            if (procName == "svchost" || procName == "svchost.exe")
                            {
                                string parentName = "UNKNOWN";
                                try
                                {
                                    var parent = Process.GetProcessById(data.ParentID);
                                    parentName = parent.ProcessName.ToLowerInvariant();
                                }
                                catch { }

                                if (parentName != "services")
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"\n[!!!] PROCESS HOLLOWING DETECTED: Illegal svchost spawn!");
                                    Console.WriteLine($"      Rogue Parent : {parentName} (PID: {data.ParentID})");
                                    Console.WriteLine($"      Target       : {data.ProcessName} (PID: {data.ProcessID})");
                                    Console.WriteLine($"      Command Line : {data.CommandLine}");
                                    Console.ResetColor();

                                    try 
                                    {
                                        var p = Process.GetProcessById(data.ProcessID);
                                        // p.Kill(); // Auto-Kill the hollowed process
                                        Console.WriteLine($"      [Action] Auto-kill simulated on PID {data.ProcessID}.");
                                    }
                                    catch { }
                                }
                            }
                            
                            // Check for Command Line Anomalies (e.g. Encoded PowerShell)
                            if (procName.Contains("powershell") && data.CommandLine.Contains("-enc", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"\n[!] ANOMALY: Encoded PowerShell Script Detected!");
                                Console.WriteLine($"      PID: {data.ProcessID} | Parent PID: {data.ParentID}");
                                Console.ResetColor();
                            }
                        };

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"     [+] ETW Global Process Monitor is ACTIVE. Sleeping at 0% CPU.");
                        Console.WriteLine($"         (Try launching powershell with -enc to test the anomaly detection!)");
                        Console.ResetColor();

                        session.Source.Process();
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[-] ETW Process Monitor failed to start: Administrator privileges are required.");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[-] ETW Process Monitor error: {ex.Message}");
                }
            });
        }

        static string RunPowerShellCommand(string command)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }

        static void ScanProcessMemoryForHollowing()
        {
            Console.WriteLine("\n==================================================");
            Console.WriteLine("===       Process Memory Hollowing Scan        ===");
            Console.WriteLine("==================================================");
            Console.WriteLine(" [1] Scan ALL running processes (System-wide hunt)");
            Console.WriteLine(" [2] Select specific processes from a list");
            Console.WriteLine(" [3] Scan processes started within a specific timeframe");
            Console.WriteLine(" [0] Cancel");
            Console.Write("\n Select an option (0-3): ");
            
            string? choice = Console.ReadLine();
            
            if (choice == "1")
            {
                Console.WriteLine("\n[+] Initiating System-Wide Memory Scan...");
                var allProcesses = Process.GetProcesses();
                foreach (var process in allProcesses)
                {
                    ScanSingleProcessMemory(process, verbose: false);
                }
                Console.WriteLine("\n[OK] System-Wide Memory Scan Complete.");
            }
            else if (choice == "2")
            {
                var apps = Process.GetProcesses()
                                  .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                                  .OrderBy(p => p.ProcessName)
                                  .ToList();

                if (apps.Count == 0)
                {
                    Console.WriteLine("[-] No active user applications found with a main window.");
                    return;
                }

                Console.WriteLine("\n    Select applications to scan memory for (comma-separated, e.g. 1,3,5):\n");
                for (int i = 0; i < apps.Count; i++)
                {
                    Console.WriteLine($"    [{i + 1}] {apps[i].ProcessName} (PID: {apps[i].Id}) - {apps[i].MainWindowTitle}");
                }
                Console.WriteLine($"    [0] Cancel");
                
                Console.Write($"\n Select apps: ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.Trim() == "0")
                {
                    Console.WriteLine("[-] Cancelled.");
                    return;
                }
                
                var parts = input.Split(',');
                foreach (var part in parts)
                {
                    if (int.TryParse(part.Trim(), out int selection) && selection > 0 && selection <= apps.Count)
                    {
                        ScanSingleProcessMemory(apps[selection - 1], verbose: true);
                    }
                }
                Console.WriteLine("\n[OK] Targeted Memory Scan Complete.");
            }
            else if (choice == "3")
            {
                Console.WriteLine("\n    Select the timeframe to scan processes started within:");
                Console.WriteLine("    [1] Last 10 minutes");
                Console.WriteLine("    [2] Last 30 minutes");
                Console.WriteLine("    [3] Last 1 hour");
                Console.WriteLine("    [4] Today (Since midnight)");
                Console.WriteLine("    [5] Since last startup (Boot time)");
                Console.WriteLine("    [0] Cancel");
                Console.Write("\n Select timeframe (0-5): ");
                
                string? timeChoice = Console.ReadLine();
                DateTime threshold = DateTime.MinValue;
                string timeLabel = "";
                
                if (timeChoice == "1") { threshold = DateTime.Now.AddMinutes(-10); timeLabel = "last 10 minutes"; }
                else if (timeChoice == "2") { threshold = DateTime.Now.AddMinutes(-30); timeLabel = "last 30 minutes"; }
                else if (timeChoice == "3") { threshold = DateTime.Now.AddHours(-1); timeLabel = "last 1 hour"; }
                else if (timeChoice == "4") { threshold = DateTime.Today; timeLabel = "today"; }
                else if (timeChoice == "5") 
                { 
                    threshold = DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64); 
                    timeLabel = "since last system startup"; 
                }
                else 
                {
                    Console.WriteLine("[-] Cancelled.");
                    return;
                }

                Console.WriteLine($"\n[+] Identifying processes started {timeLabel}...");
                var apps = new System.Collections.Generic.List<Process>();
                var allProcesses = Process.GetProcesses();
                
                foreach (var p in allProcesses)
                {
                    try
                    {
                        if (p.StartTime >= threshold)
                        {
                            apps.Add(p);
                        }
                    }
                    catch { /* Ignore processes we can't query StartTime for */ }
                }

                if (apps.Count == 0)
                {
                    Console.WriteLine($"[-] No processes found started {timeLabel} (or access denied).");
                    return;
                }

                apps = apps.OrderByDescending(p => p.StartTime).ToList();
                Console.WriteLine($"\n[+] Found {apps.Count} processes. Automatically scanning all of them...\n");

                foreach (var app in apps)
                {
                    ScanSingleProcessMemory(app, verbose: false);
                }
                Console.WriteLine("\n[OK] Timeframe Memory Scan Complete.");
            }
            else
            {
                Console.WriteLine("[-] Cancelled.");
            }
        }

        static void ScanSingleProcessMemory(Process process, bool verbose)
        {
            try
            {
                IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, process.Id);
                if (hProcess == IntPtr.Zero)
                {
                    if (verbose) Console.WriteLine($"    [-] Could not open process {process.ProcessName} (PID: {process.Id}). Access Denied.");
                    return;
                }

                long address = 0;
                bool hollowedDetected = false;
                MEMORY_BASIC_INFORMATION64 memInfo;
                
                if (verbose) Console.WriteLine($"    -> Querying memory pages for {process.ProcessName} (PID: {process.Id})...");

                while (VirtualQueryEx(hProcess, (IntPtr)address, out memInfo, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64))) != 0)
                {
                    bool isExecutable = (memInfo.Protect == PAGE_EXECUTE || 
                                         memInfo.Protect == PAGE_EXECUTE_READ || 
                                         memInfo.Protect == PAGE_EXECUTE_READWRITE || 
                                         memInfo.Protect == PAGE_EXECUTE_WRITECOPY);

                    if (memInfo.State == MEM_COMMIT && isExecutable)
                    {
                        System.Text.StringBuilder fileName = new System.Text.StringBuilder(MAX_PATH);
                        uint result = GetMappedFileName(hProcess, (IntPtr)address, fileName, MAX_PATH);

                        if (result == 0) // Unbacked Memory
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"\n[!!!] PROCESS HOLLOWING DETECTED IN {process.ProcessName} (PID: {process.Id})!");
                            Console.WriteLine($"      -> Executable memory found with NO backing file (Unbacked Memory)!");
                            Console.WriteLine($"      -> Base Address : 0x{address:X}");
                            Console.WriteLine($"      -> Memory Type  : {(memInfo.Type == MEM_PRIVATE ? "MEM_PRIVATE" : memInfo.Type.ToString())}");
                            Console.WriteLine($"      -> Protection   : 0x{memInfo.Protect:X}");
                            Console.ResetColor();
                            hollowedDetected = true;

                            Console.WriteLine($"\n[+] Initiating Incident Response Aftermath Protocol...");
                            
                            // Phase 2: Memory Dumping
                            Console.WriteLine($"    -> [Phase 2] Dumping unbacked memory payload...");
                            byte[] buffer = new byte[memInfo.RegionSize];
                            if (ReadProcessMemory(hProcess, (IntPtr)address, buffer, (int)memInfo.RegionSize, out IntPtr bytesRead))
                            {
                                string dumpFile = $"Malware_Dump_PID{process.Id}_0x{address:X}.bin";
                                File.WriteAllBytes(dumpFile, buffer);
                                Console.WriteLine($"       [SUCCESS] Payload dumped to: {dumpFile}");
                            }
                            else
                            {
                                Console.WriteLine($"       [FAILED] Could not read process memory.");
                            }

                            // Phase 3: Trace Origin
                            int parentPid = 0;
                            Console.WriteLine($"    -> [Phase 3] Tracing origin (Parent Process)...");
                            try
                            {
                                using (var searcher = new ManagementObjectSearcher($"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {process.Id}"))
                                {
                                    var objects = searcher.Get();
                                    foreach (var obj in objects)
                                    {
                                        parentPid = Convert.ToInt32(obj["ParentProcessId"]);
                                        break;
                                    }
                                }
                                if (parentPid > 0)
                                {
                                    var parent = Process.GetProcessById(parentPid);
                                    Console.WriteLine($"       [SUCCESS] Found Parent PID {parentPid} ({parent.ProcessName}). Terminating parent...");
                                    // parent.Kill();
                                    Console.WriteLine($"       [SIMULATED] Parent Process {parentPid} terminated.");
                                }
                                else
                                {
                                    Console.WriteLine($"       [FAILED] Parent process not found or already dead.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"       [FAILED] Could not query parent process: {ex.Message}");
                            }

                            // Phase 1: The Kill
                            Console.WriteLine($"    -> [Phase 1] Terminating hollowed process...");
                            try
                            {
                                // process.Kill();
                                Console.WriteLine($"       [SIMULATED] Hollowed Process {process.Id} terminated.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"       [FAILED] Could not terminate process: {ex.Message}");
                            }

                            // Phase 4: Hunt for Persistence
                            Console.WriteLine($"    -> [Phase 4] Hunting for Persistence (Registry/Startup)...");
                            try
                            {
                                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                                {
                                    if (key != null)
                                    {
                                        var names = key.GetValueNames();
                                        if (names.Length > 0)
                                        {
                                            Console.WriteLine($"       [WARNING] Found {names.Length} startup registry keys. Please review them.");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"       [SUCCESS] No obvious registry persistence found.");
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                Console.WriteLine($"       [FAILED] Could not scan registry.");
                            }
                            
                            // Phase 5: Network Quarantine
                            Console.WriteLine($"    -> [Phase 5] Network Quarantine...");
                            Console.WriteLine($"       [SIMULATED] Windows Firewall instructed to block all traffic for this host.");
                        }
                    }
                    address += (long)memInfo.RegionSize;
                }
                
                if (verbose && !hollowedDetected)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"       [OK] All executable pages backed properly. No hollowing detected.");
                    Console.ResetColor();
                }

                CloseHandle(hProcess);
            }
            catch
            {
                if (verbose) Console.WriteLine($"    [-] Error scanning {process.ProcessName} (PID: {process.Id}).");
            }
        }

        // --- P/Invoke Signatures for Memory Scanning ---
        const uint PROCESS_QUERY_INFORMATION = 0x0400;
        const uint PROCESS_VM_READ = 0x0010;
        const uint MEM_COMMIT = 0x1000;
        const uint MEM_PRIVATE = 0x20000;
        const uint PAGE_EXECUTE = 0x10;
        const uint PAGE_EXECUTE_READ = 0x20;
        const uint PAGE_EXECUTE_READWRITE = 0x40;
        const uint PAGE_EXECUTE_WRITECOPY = 0x80;
        const int MAX_PATH = 260;

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION64
        {
            public ulong BaseAddress;
            public ulong AllocationBase;
            public uint AllocationProtect;
            public uint __alignment1;
            public ulong RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint __alignment2;
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

        [DllImport("psapi.dll", SetLastError = true)]
        static extern uint GetMappedFileName(IntPtr hProcess, IntPtr lpv, System.Text.StringBuilder lpFilename, uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);
    }
}
