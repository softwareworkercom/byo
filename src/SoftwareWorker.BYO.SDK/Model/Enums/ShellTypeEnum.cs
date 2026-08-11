namespace SoftwareWorker.BYO.Core.Model.Enums
{
    /// <summary>
    /// Supported shell types for command execution.
    /// </summary>
    public enum ShellTypeEnum
    {
        /// <summary>
        /// PowerShell (default on Windows)
        /// </summary>
        PowerShell,

        /// <summary>
        /// Command Prompt (cmd.exe)
        /// </summary>
        Cmd,

        /// <summary>
        /// Windows Subsystem for Linux (wsl.exe)
        /// </summary>
        Wsl,

        /// <summary>
        /// Bash shell (default on Linux/macOS)
        /// </summary>
        Bash,

        /// <summary>
        /// Zsh shell (macOS default)
        /// </summary>
        Zsh,

        /// <summary>
        /// Bourne shell (Unix/Linux)
        /// </summary>
        Sh
    }
}
