using SoftwareWorker.BYO.CLI.Core.Service;
using System.Diagnostics;
using System.Security.Principal;

namespace SoftwareWorker.BYO.CLI.Core.Helpers
{
    public static class ProcessHelper
    {
        public static void KillProcess(string process)
        {
            Process[] workers = Process.GetProcessesByName(process);
            foreach (var worker in workers)
            {
                worker.Kill();
                worker.WaitForExit();
                worker.Dispose();
            }
            UserInterfaceService.ShowError($"Process {process} has been terminated.");
        }

        public static void KillProcesses(List<string> processes)
        {
            foreach (var process in processes)
            {
                KillProcess(process);
            }
        }

        public static bool IsRunningAsAdmin()
        {
            if (OperatingSystem.IsWindows())
            {
                var currentUser = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(currentUser);
                bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                return isAdmin;
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // On Unix-like systems, check if user is root (UID = 0)
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "id", // Let PATH resolve the location
                        Arguments = "-u",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd().Trim();
                            process.WaitForExit();
                            return output == "0"; // UID 0 is root
                        }
                    }
                }
                catch
                {
                    // If we can't determine, assume not admin
                    return false;
                }
            }

            // Unknown platform, assume not admin
            return false;
        }

        public static int? FindProcessId(string processName)
        {
            var pid = Process.GetProcessesByName(processName).FirstOrDefault().Id;
            UserInterfaceService.ShowWarning($"Process ID for {processName}.exe: {pid}");
            return pid;
        }

    }
}
