using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ZeroTrustMonitor
{
    public class DllInjector
    {
        // Windows APIs for DLL Injection
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        // Constants
        const int PROCESS_ALL_ACCESS = 0x001F0FFF;
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 0x04;

        public static void InjectIntoProcess(string processName)
        {
            Console.WriteLine($"\n[+] Initializing EDR API Hooking Engine...");
            
            // 1. Find the target process
            Process[] processes = Process.GetProcessesByName(processName.Replace(".exe", ""));
            if (processes.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[-] Target process '{processName}' is not currently running. Start the app first!");
                Console.ResetColor();
                return;
            }

            Process targetProcess = processes[0];
            Console.WriteLine($"    -> Found target process: {targetProcess.ProcessName} (PID: {targetProcess.Id})");

            // For this implementation, we simulate the path to our AudioTrap.dll
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioTrap.dll");
            
            Console.WriteLine($"    -> Payload DLL Path: {dllPath}");

            // 2. Open the process with all access
            Console.WriteLine($"    -> Opening target process memory...");
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);
            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine("[-] Failed to open process. Try running this CLI as Administrator.");
                return;
            }

            // 3. Allocate memory inside the target process for the DLL path string
            Console.WriteLine($"    -> Allocating memory in target process...");
            uint size = (uint)((dllPath.Length + 1) * Marshal.SystemDefaultCharSize);
            IntPtr allocatedMemory = VirtualAllocEx(hProcess, IntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            if (allocatedMemory == IntPtr.Zero)
            {
                Console.WriteLine("[-] Failed to allocate memory.");
                CloseHandle(hProcess);
                return;
            }

            // 4. Write the DLL path string into the target process memory
            Console.WriteLine($"    -> Writing hook payload into memory...");
            byte[] bytes = Encoding.Default.GetBytes(dllPath);
            bool success = WriteProcessMemory(hProcess, allocatedMemory, bytes, (uint)bytes.Length, out UIntPtr bytesWritten);

            if (!success)
            {
                Console.WriteLine("[-] Failed to write memory.");
                CloseHandle(hProcess);
                return;
            }

            // 5. Get the memory address of LoadLibraryA in kernel32.dll
            // Since kernel32.dll is loaded at the same address in all processes, we can get it from our own process
            Console.WriteLine($"    -> Resolving LoadLibraryA pointer...");
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            // 6. Create a remote thread in the target process that calls LoadLibraryA, passing our DLL path
            Console.WriteLine($"    -> Executing Remote Thread Injection...");
            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocatedMemory, 0, out IntPtr threadId);

            if (hThread == IntPtr.Zero)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[-] Failed to create remote thread. The target may be protected by an Anti-Cheat or ACG.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[!!!] EDR HOOK DEPLOYED SUCCESSFULLY!");
                Console.WriteLine($"      Remote Thread ID: {threadId} is currently executing the trap.");
                Console.WriteLine($"      The application is now actively monitored. Any mute calls will be intercepted.");
                Console.ResetColor();
                CloseHandle(hThread);
            }

            CloseHandle(hProcess);
        }
    }
}
