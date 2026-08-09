using System;
using System.Runtime.InteropServices;

namespace AudioTrap
{
    public class Trap
    {
        // 1 = DLL_PROCESS_ATTACH, 2 = DLL_THREAD_ATTACH, 3 = DLL_THREAD_DETACH, 0 = DLL_PROCESS_DETACH
        [UnmanagedCallersOnly(EntryPoint = "DllMain")]
        public static bool DllMain(IntPtr hModule, uint ul_reason_for_call, IntPtr lpReserved)
        {
            if (ul_reason_for_call == 1) // DLL_PROCESS_ATTACH
            {
                // We are inside the target process! 
                // In a production EDR, we would spawn a background thread here and use MinHook
                // to rewrite the memory of ISimpleAudioVolume::SetMute.
                
                string logMessage = $"\n[!] EDR SENSOR INJECTED SUCESSFULLY! Time: {DateTime.Now:HH:mm:ss.fff}\n" +
                                    $"    -> Process ID: {System.Diagnostics.Process.GetCurrentProcess().Id}\n" +
                                    $"    -> Native AOT DLL is actively running inside the target's memory space.\n";
                
                // We write to a log file on the desktop as proof of injection since we don't have a console here
                string logPath = @"C:\ProcessLog.txt";
                try
                {
                    System.IO.File.AppendAllText(logPath, logMessage);
                }
                catch { } // Ignore errors if we don't have write access
            }
            return true;
        }
    }
}
