using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.Core.Model.Enums;
using System.Diagnostics;
using System.Text;
using Process = System.Diagnostics.Process;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class TerminalService
    {
        public static int? Run(
            string command,
            string? directory = null,
            bool isMultipleExecution = false,
            ShellTypeEnum? shell = null,
            bool runAsync = false)
        {
            try
            {
                //Resolve Tokens
                command = TokenService.ResolveTokens(command);
                directory = directory != null ? TokenService.ResolveTokens(directory) : null;

                DisplayCommandInformation(command, directory);

                //Configure Prompt based on shell parameter (default to PowerShell)
                var (filename, arguments) = DetermineShell(command, shell);

                //Configure ProcessStartInfo    
                var processStartInfo = ConfigureProcessStartInfo(directory, filename, arguments, isMultipleExecution, runAsync);

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    string output = string.Empty;
                    string error = string.Empty;
                    StringBuilder? outputBuilder = null;
                    StringBuilder? errorBuilder = null;

                    if (processStartInfo.RedirectStandardOutput)
                    {
                        outputBuilder = new StringBuilder();
                        errorBuilder = new StringBuilder();

                        process.OutputDataReceived += (_, e) =>
                        {
                            if (e.Data == null)
                            {
                                return;
                            }

                            outputBuilder.AppendLine(e.Data);
                            UserInterfaceService.WriteLine(e.Data);
                        };

                        process.ErrorDataReceived += (_, e) =>
                        {
                            if (e.Data == null)
                            {
                                return;
                            }

                            errorBuilder.AppendLine(e.Data);
                            UserInterfaceService.ShowError(e.Data);
                        };
                    }

                    process.Start();

                    if (runAsync)
                    {
                        UserInterfaceService.ShowGrey($"Started asynchronously (PID {process.Id}).");
                        return process.Id;
                    }

                    if (processStartInfo.RedirectStandardOutput)
                    {
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }

                    process.WaitForExit();

                    if (processStartInfo.RedirectStandardOutput)
                    {
                        output = outputBuilder?.ToString() ?? string.Empty;
                        error = errorBuilder?.ToString() ?? string.Empty;
                    }

                    AppendToLogFile(command, output, error);

                    return process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                UserInterfaceService.ShowError($"{ex.Message}");
                return null;
            }
        }

        private static (string filename, string arguments) DetermineShell(string command, ShellTypeEnum? shell)
        {
            // Default to platform-appropriate shell if not specified
            var shellToUse = shell ?? GetDefaultShell();

            return shellToUse switch
            {
                ShellTypeEnum.Cmd => ("cmd.exe", $"/c {command}"),
                ShellTypeEnum.Wsl => ("wsl.exe", $"bash -c \"{command.Replace("\"", "\\\"")}\""),
                ShellTypeEnum.PowerShell => ("powershell.exe", $"-noprofile -nologo -c {command}"),
                ShellTypeEnum.Bash => ("/bin/bash", $"-c \"{command.Replace("\"", "\\\"")}\""),
                ShellTypeEnum.Zsh => ("/bin/zsh", $"-c \"{command.Replace("\"", "\\\"")}\""),
                ShellTypeEnum.Sh => ("/bin/sh", $"-c \"{command.Replace("\"", "\\\"")}\""),
                _ => GetDefaultShellCommand(command) // Default fallback
            };
        }

        private static ShellTypeEnum GetDefaultShell()
        {
            if (OperatingSystem.IsWindows())
            {
                return ShellTypeEnum.PowerShell;
            }
            else if (OperatingSystem.IsMacOS())
            {
                // macOS uses zsh as default since Catalina
                return ShellTypeEnum.Zsh;
            }
            else
            {
                // Linux typically uses bash
                return ShellTypeEnum.Bash;
            }
        }

        private static (string filename, string arguments) GetDefaultShellCommand(string command)
        {
            if (OperatingSystem.IsWindows())
            {
                return ("powershell.exe", $"-noprofile -nologo -c {command}");
            }
            else if (OperatingSystem.IsMacOS())
            {
                return ("/bin/zsh", $"-c \"{command.Replace("\"", "\\\"")}\"");
            }
            else
            {
                return ("/bin/bash", $"-c \"{command.Replace("\"", "\\\"")}\"");
            }
        }

        private static ProcessStartInfo ConfigureProcessStartInfo(
            string? directory,
            string filename,
            string arguments,
            bool isMultipleExecution,
            bool runAsync)
        {
            var shouldDetach = isMultipleExecution || runAsync;

            var processStartInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                RedirectStandardOutput = !shouldDetach,
                RedirectStandardError = !shouldDetach,
                UseShellExecute = shouldDetach
            };

            if (!string.IsNullOrEmpty(directory))
            {
                processStartInfo.WorkingDirectory = directory;
            }

            if (shouldDetach)
            {
                processStartInfo.CreateNoWindow = false;
            }

            return processStartInfo;
        }

        private static void AppendToLogFile(string command, string output, string error)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Command: {command}{Environment.NewLine}";

            if (!string.IsNullOrWhiteSpace(output))
            {
                logEntry += $"Output:{Environment.NewLine}{output}{Environment.NewLine}";
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                logEntry += $"Error:{Environment.NewLine}{error}{Environment.NewLine}";
            }

            logEntry += new string('-', 80) + Environment.NewLine;

            var logFilePath = Path.Combine(SystemConstants.STORAGE_DIRECTORY, "logs.txt");
            Directory.CreateDirectory(SystemConstants.STORAGE_DIRECTORY);
            File.AppendAllText(logFilePath, logEntry);
        }

        private static void DisplayCommandOutput(string output, string error)
        {
            if (!string.IsNullOrWhiteSpace(output))
            {
                UserInterfaceService.WriteLine(output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                UserInterfaceService.ShowError(error);
            }
        }

        private static void DisplayCommandInformation(string command, string? directory)
        {
            UserInterfaceService.WriteLine();
            UserInterfaceService.ShowBlue($"Command: {command}");
            if (!string.IsNullOrEmpty(directory))
            {
                UserInterfaceService.ShowBlue($"Directory: {directory}");
            }
            else
            {
                UserInterfaceService.ShowBlue($"Directory: {Environment.CurrentDirectory}");
            }
        }
    }
}


