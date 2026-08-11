using System.Runtime.InteropServices;

namespace SoftwareWorker.BYO.CLI.Core.Helpers
{
    /// <summary>
    /// Helper class for generating cross-platform machine identifiers.
    /// </summary>
    public static class MachineIdentifierHelper
    {
        /// <summary>
        /// Generates a unique machine identifier using platform-appropriate system information.
        /// This combines various system properties to create a machine-specific identifier.
        /// </summary>
        /// <returns>A pipe-separated string of machine identifiers</returns>
        public static string GetMachineIdentifier()
        {
            // Use cross-platform system information
            var machineInfo = new List<string>
            {
                Environment.MachineName,
                Environment.UserName,
                RuntimeInformation.OSDescription,
                RuntimeInformation.OSArchitecture.ToString(),
                RuntimeInformation.FrameworkDescription,
                Environment.ProcessorCount.ToString()
            };

            // Add platform-specific identifiers
            if (OperatingSystem.IsWindows())
            {
                // On Windows, try to get more specific machine info
                machineInfo.Add(Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "");
                machineInfo.Add(Environment.GetEnvironmentVariable("PROCESSOR_REVISION") ?? "");
            }
            else if (OperatingSystem.IsLinux())
            {
                // On Linux, try to read machine-id
                try
                {
                    if (File.Exists("/etc/machine-id"))
                    {
                        machineInfo.Add(File.ReadAllText("/etc/machine-id").Trim());
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Cannot read /etc/machine-id, skip it
                }
                catch (IOException)
                {
                    // File error, skip it
                }

                try
                {
                    if (File.Exists("/var/lib/dbus/machine-id"))
                    {
                        machineInfo.Add(File.ReadAllText("/var/lib/dbus/machine-id").Trim());
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Cannot read machine-id, skip it
                }
                catch (IOException)
                {
                    // File error, skip it
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                // On macOS, we could use IOPlatformUUID but that requires additional APIs
                // For now, the machine name and user should provide reasonable uniqueness
                machineInfo.Add(Environment.GetEnvironmentVariable("USER") ?? "");
            }

            // Combine all identifiers with a separator
            return string.Join("|", machineInfo);
        }
    }
}
