using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CustomApiMonitor
{
    class Program
    {
        // 1. Define the necessary Windows API functions for Debugging
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool DebugActiveProcess(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WaitForDebugEvent(out DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ContinueDebugEvent(uint dwProcessId, uint dwThreadId, uint dwContinueStatus);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool DebugActiveProcessStop(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, uint nSize);

        // 2. Define the Debug Event structures
        [StructLayout(LayoutKind.Sequential)]
        public struct DEBUG_EVENT
        {
            public uint dwDebugEventCode;
            public uint dwProcessId;
            public uint dwThreadId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 86, ArraySubType = UnmanagedType.U1)]
            public byte[] u; // Union containing event details. Size is a safe overestimate.
        }

        const uint EXCEPTION_DEBUG_EVENT = 1;
        const uint EXCEPTION_BREAKPOINT = 0x80000003;
        const uint DBG_CONTINUE = 0x00010002;
        const uint DBG_EXCEPTION_NOT_HANDLED = 0x80010001;
        const uint PROCESS_ALL_ACCESS = 0x001F0FFF;
        const uint INFINITE = 0xFFFFFFFF;

        public static void RunApiMonitor(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("===   Custom API Monitor (Debugger Concept)    ===");
            Console.WriteLine("==================================================\n");

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: CustomApiMonitor.exe <Process_ID>");
                return;
            }

            if (!int.TryParse(args[0], out int targetPid))
            {
                Console.WriteLine("Invalid Process ID.");
                return;
            }

            Console.WriteLine($"[1] Attempting to attach debugger to PID: {targetPid}...");

            // Step 1: Attach to the target process as a debugger
            if (!DebugActiveProcess(targetPid))
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"[-] Failed to attach. Error code: {error}");
                Console.WriteLine("    Make sure you are running as Administrator and the target is not a protected system process.");
                return;
            }

            Console.WriteLine("[+] Successfully attached! System is now intercepting thread events.");
            Console.WriteLine("[2] Waiting for events (Ctrl+C to detach)...");

            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, targetPid);

            // Step 2: The Debug Loop (This is how API Monitors freeze and inspect threads)
            // In a real API monitor, we would now inject an INT 3 (0xCC) breakpoint at the address of ISimpleAudioVolume::SetMute.
            // For this conceptual example, we will just listen for standard debug events.
            
            bool isDebugging = true;
            Console.CancelKeyPress += (sender, e) => {
                isDebugging = false;
                e.Cancel = true; // Prevent immediate exit so we can detach cleanly
            };

            while (isDebugging)
            {
                if (WaitForDebugEvent(out DEBUG_EVENT debugEvent, 1000)) // Wait 1 second
                {
                    uint continueStatus = DBG_CONTINUE;

                    // Step 3: Handle the events. When a breakpoint is hit, the thread is frozen.
                    if (debugEvent.dwDebugEventCode == EXCEPTION_DEBUG_EVENT)
                    {
                        // The 'u' byte array contains the EXCEPTION_RECORD.
                        // We would parse it here to see if the ExceptionCode is EXCEPTION_BREAKPOINT (0x80000003).
                        // If it is, the thread is currently FROZEN.
                        
                        Console.WriteLine($"\n[!] EXCEPTION EVENT in Thread ID: {debugEvent.dwThreadId}");
                        Console.WriteLine($"    -> Thread is currently suspended. In a full implementation, we would now:");
                        Console.WriteLine($"       1. Read the stack using ReadProcessMemory to find the return address.");
                        Console.WriteLine($"       2. Read CPU registers (RCX/RDX) using GetThreadContext to view arguments.");
                        
                        // We must pass DBG_EXCEPTION_NOT_HANDLED if it's an exception we didn't cause,
                        // otherwise the application might crash.
                        continueStatus = DBG_EXCEPTION_NOT_HANDLED; 
                    }
                    else
                    {
                        // Print out other events (Process Create, Thread Create, DLL Load, etc.)
                        Console.WriteLine($"[*] Event Code: {debugEvent.dwDebugEventCode} | PID: {debugEvent.dwProcessId} | TID: {debugEvent.dwThreadId}");
                    }

                    // Step 4: Resume the frozen thread
                    ContinueDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, continueStatus);
                }
            }

            // Step 5: Detach safely when done
            Console.WriteLine("\n[3] Detaching debugger...");
            DebugActiveProcessStop((uint)targetPid);
            Console.WriteLine("[+] Detached. Target process resumed normal execution.");
        }
    }
}
