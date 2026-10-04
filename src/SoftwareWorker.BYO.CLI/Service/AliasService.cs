using SoftwareWorker.BYO.SDK.Constants;
using SoftwareWorker.BYO.SDK.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SoftwareWorker.BYO.CLI.Service
{
    public static class AliasService
    {
        private static readonly Regex ValidAlias = new(@"^[A-Za-z][A-Za-z0-9_-]*$", RegexOptions.Compiled);

        private const string AliasPlaceholder = "{alias}";

        private static readonly TimeSpan DeployTimeout = TimeSpan.FromSeconds(30);

        // Used by PowerShell 7+ (Windows, Linux, macOS) and Windows PowerShell 5.1; each writes to its own $PROFILE.
        private const string PowerShellDeployScriptTemplate = """
            if (-not (Test-Path -LiteralPath $PROFILE)) { New-Item -ItemType File -Path $PROFILE -Force | Out-Null }
            Add-Content -LiteralPath $PROFILE -Value "`nSet-Alias {alias} byo"
            """;

        // cmd has no profile: the alias is a doskey macro, loaded through the Command Processor AutoRun value.
        // An existing AutoRun command is kept and the macro file is chained after it.
        private const string CmdDeployScriptTemplate = """
            @echo off
            setlocal EnableDelayedExpansion
            set "MACROS=%USERPROFILE%\.byo_aliases.doskey"
            findstr /b /c:"{alias}=" "%MACROS%" >nul 2>&1 || >>"%MACROS%" echo {alias}=byo $*
            set "KEY=HKCU\Software\Microsoft\Command Processor"
            set "LOADER=doskey /macrofile="%MACROS%""
            set "CURRENT="
            set "TYPE=REG_SZ"
            for /f "tokens=1,2,*" %%a in ('reg query "%KEY%" /v AutoRun 2^>nul') do if /i "%%a"=="AutoRun" (
                set "TYPE=%%b"
                set "CURRENT=%%c"
            )
            if defined CURRENT (
                if not "!CURRENT:.byo_aliases.doskey=!"=="!CURRENT!" exit /b 0
                set "LOADER=!CURRENT! & !LOADER!"
            )
            reg add "%KEY%" /v AutoRun /t !TYPE! /d "!LOADER:"=\"!" /f >nul
            """;

        // Git Bash on Windows and bash on Linux read ~/.bashrc. Terminals on macOS start login shells,
        // so use the login file bash already reads instead of creating one that would shadow ~/.profile.
        private const string BashDeployScriptTemplate = """
            rc="$HOME/.bashrc"
            if [ "$(uname -s)" = "Darwin" ]; then
                rc="$HOME/.bash_profile"
                for f in "$HOME/.bash_profile" "$HOME/.bash_login" "$HOME/.profile"; do
                    if [ -f "$f" ]; then rc="$f"; break; fi
                done
            fi
            printf '\nalias {alias}="byo"\n' >> "$rc"
            """;

        private const string ZshDeployScriptTemplate = """
            printf '\nalias {alias}="byo"\n' >> "${ZDOTDIR:-$HOME}/.zshrc"
            """;

        private const string FishDeployScriptTemplate = """
            if set -q XDG_CONFIG_HOME
                set dir $XDG_CONFIG_HOME/fish
            else
                set dir $HOME/.config/fish
            end
            mkdir -p $dir
            printf '\nalias {alias} byo\n' >> $dir/config.fish
            """;

        private sealed record AliasShell(
            string Name,
            Func<string?> Locate,
            string[] Arguments,
            string ScriptExtension,
            string DeployScriptTemplate);

        private static readonly AliasShell[] Shells =
        [
            new("PowerShell", () => FindOnPath("pwsh"), PowerShellArguments(), ".ps1", PowerShellDeployScriptTemplate),
            new("Windows PowerShell", () => OperatingSystem.IsWindows() ? FindOnPath("powershell") : null, PowerShellArguments(), ".ps1", PowerShellDeployScriptTemplate),
            new("cmd", () => OperatingSystem.IsWindows() ? Environment.GetEnvironmentVariable("ComSpec") ?? FindOnPath("cmd") : null, ["/d", "/c"], ".cmd", CmdDeployScriptTemplate),
            new("bash", () => OperatingSystem.IsWindows() ? FindGitBash() : FindOnPath("bash"), [], ".sh", BashDeployScriptTemplate),
            new("zsh", () => OperatingSystem.IsWindows() ? null : FindOnPath("zsh"), [], ".zsh", ZshDeployScriptTemplate),
            new("fish", () => OperatingSystem.IsWindows() ? null : FindOnPath("fish"), ["--no-config"], ".fish", FishDeployScriptTemplate)
        ];

        /// <summary>
        /// On an interactive run, offers to set a shell alias for byo when none is stored in settings
        /// and the user has not previously declined.
        /// </summary>
        public static void EnsureAliasConfigured()
        {
            if (!UserInterfaceService.IsInteractive)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(SettingsService.Get(SystemConstants.SYSTEM_Alias, showErrorIfNotFound: false)) ||
                SettingsService.GetBoolean(SystemConstants.SYSTEM_SkipAliasPrompt))
            {
                return;
            }

            if (!UserInterfaceService.Confirm("No alias is set for byo. Do you want to set one?"))
            {
                SettingsService.Update(SystemConstants.SYSTEM_SkipAliasPrompt, "true");
                UserInterfaceService.ShowGrey($"You will not be asked again. Set '{SystemConstants.SYSTEM_Alias}' in settings to configure one later.");
                return;
            }

            var alias = AskAlias();

            if (SettingsService.Update(SystemConstants.SYSTEM_Alias, alias) == null)
            {
                return;
            }

            Deploy(alias);
        }

        public static string GetPromptName()
        {
            var alias = SettingsService.Get(SystemConstants.SYSTEM_Alias, showErrorIfNotFound: false);
            return string.IsNullOrWhiteSpace(alias) ? "byo" : alias.Trim();
        }

        public static string AskAliasByUser()
        {
            return AskAlias();
        }

        public static void DeployAlias(string alias)
        {
            Deploy(alias);
        }

        private static string AskAlias()
        {
            while (true)
            {
                var alias = UserInterfaceService.AskInput("Alias").Trim();

                if (ValidAlias.IsMatch(alias))
                {
                    return alias;
                }

                UserInterfaceService.ShowError("Alias must start with a letter and contain only letters, digits, '-' or '_'.");
            }
        }

        private static void Deploy(string alias)
        {
            var deployed = new List<string>();
            var found = false;

            foreach (var shell in Shells)
            {
                var executable = shell.Locate();
                if (executable == null)
                {
                    continue;
                }

                found = true;
                var error = RunDeployScript(shell, executable, alias);

                if (error == null)
                {
                    deployed.Add(shell.Name);
                }
                else
                {
                    UserInterfaceService.ShowWarning($"Deploying alias '{alias}' to {shell.Name} failed: {error}");
                }
            }

            if (!found)
            {
                UserInterfaceService.ShowWarning($"Alias '{alias}' saved in settings, but no supported shell was found. Add an alias for byo named '{alias}' to your shell profile manually.");
                return;
            }

            if (deployed.Count > 0)
            {
                UserInterfaceService.ShowSuccess($"Alias '{alias}' added for {string.Join(", ", deployed)}. Open a new terminal to use it.");
            }
        }

        /// <summary>
        /// Runs the shell's deploy script for the alias. Returns null on success, otherwise the error.
        /// </summary>
        private static string? RunDeployScript(AliasShell shell, string executable, string alias)
        {
            var newLine = shell.ScriptExtension == ".cmd" ? "\r\n" : "\n";
            var script = shell.DeployScriptTemplate.Replace(AliasPlaceholder, alias).ReplaceLineEndings(newLine) + newLine;
            var scriptPath = Path.Combine(Path.GetTempPath(), $"byo-alias-{Guid.NewGuid():N}{shell.ScriptExtension}");

            try
            {
                File.WriteAllText(scriptPath, script);

                var startInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                foreach (var argument in shell.Arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }
                startInfo.ArgumentList.Add(scriptPath);

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return $"{executable} could not be started.";
                }

                process.StandardInput.Close();
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(DeployTimeout))
                {
                    process.Kill(entireProcessTree: true);
                    return $"timed out after {DeployTimeout.TotalSeconds} seconds.";
                }

                if (process.ExitCode == 0)
                {
                    return null;
                }

                var message = error.Result.Trim();
                return string.IsNullOrEmpty(message) ? $"exit code {process.ExitCode}. {output.Result.Trim()}".Trim() : message;
            }
            catch (Win32Exception ex)
            {
                return ex.Message;
            }
            catch (IOException ex)
            {
                return ex.Message;
            }
            finally
            {
                try
                {
                    File.Delete(scriptPath);
                }
                catch (IOException)
                {
                    // best effort cleanup of the temp script
                }
            }
        }

        private static string[] PowerShellArguments()
        {
            return OperatingSystem.IsWindows()
                ? ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File"]
                : ["-NoProfile", "-NonInteractive", "-File"];
        }

        /// <summary>
        /// Finds Git Bash on Windows. The bash.exe in System32 is the WSL launcher, whose home directory
        /// and PATH are separate from Windows, so it is never used.
        /// </summary>
        private static string? FindGitBash()
        {
            var candidates = new List<string>();

            var git = FindOnPath("git");
            if (git != null)
            {
                // git.exe lives in <Git>\cmd or <Git>\bin; bash.exe is in <Git>\bin.
                var gitRoot = Path.GetDirectoryName(Path.GetDirectoryName(git));
                if (gitRoot != null)
                {
                    candidates.Add(Path.Combine(gitRoot, "bin", "bash.exe"));
                }
            }

            foreach (var root in new[] { "ProgramW6432", "ProgramFiles", "ProgramFiles(x86)" })
            {
                var programFiles = Environment.GetEnvironmentVariable(root);
                if (!string.IsNullOrEmpty(programFiles))
                {
                    candidates.Add(Path.Combine(programFiles, "Git", "bin", "bash.exe"));
                }
            }

            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Git", "bin", "bash.exe"));

            var found = candidates.FirstOrDefault(File.Exists);
            if (found != null)
            {
                return found;
            }

            var bash = FindOnPath("bash");
            return bash != null && !bash.StartsWith(Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase) && !bash.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase)
                ? bash
                : null;
        }

        private static string? FindOnPath(string name)
        {
            var fileName = OperatingSystem.IsWindows() ? name + ".exe" : name;
            var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            foreach (var path in paths)
            {
                try
                {
                    var candidate = Path.Combine(path.Trim('"'), fileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch (ArgumentException)
                {
                    // malformed PATH entry
                }
            }

            return null;
        }
    }
}