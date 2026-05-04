using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.Core.Model.Enums;
using SoftwareWorker.BYO.Core.Storage;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class CommandService
    {
        public static string CommandsFilePath { get; set; } = SystemConstants.STORAGE_COMMANDS_FILE;

        /// <summary>
        /// Creates a new command and adds it to storage.
        /// </summary>
        /// <param name="description">The description of the command.</param>
        /// <param name="executable">The executable command string.</param>
        /// <param name="workingDirectory">Optional working directory for the command.</param>
        /// <param name="shell">Optional shell type for the command.</param>
        /// <param name="folderPath">Optional hierarchical folder path (e.g. "DevOps/Deploy").</param>
        /// <returns>The created command.</returns>
        public static ShellCommand Create(string description, string executable, string? workingDirectory = null, ShellTypeEnum? shell = null, string? folderPath = null)
        {
            var commands = StorageService.LoadList<ShellCommand>(CommandsFilePath);

            var command = new ShellCommand
            {
                Name = description,
                Executable = executable,
                Directory = workingDirectory,
                Shell = shell,
                Bookmark = NormalizeFolderPath(folderPath),
                CreatedAt = DateTime.UtcNow
            };

            commands.Add(command);
            commands = commands.OrderBy(c => c.Name).ToList();

            StorageService.SaveList<ShellCommand>(CommandsFilePath, commands);

            return command;
        }

        /// <summary>
        /// Updates an existing command.
        /// </summary>
        /// <param name="description">The description of the command to update.</param>
        /// <param name="newDescription">Optional new description.</param>
        /// <param name="executable">Optional new executable.</param>
        /// <param name="workingDirectory">Optional new working directory.</param>
        /// <param name="shell">Optional new shell type.</param>
        /// <param name="folderPath">Optional new folder path.</param>
        /// <returns>The updated command if found, null otherwise.</returns>
        public static ShellCommand? Update(
            string description,
            string? newDescription = null,
            string? executable = null,
            string? workingDirectory = null,
            ShellTypeEnum? shell = null,
            string? folderPath = null)
        {
            var commands = StorageService.LoadList<ShellCommand>(CommandsFilePath);
            var existingCommand = commands.FirstOrDefault(c => c.Name.Equals(description, StringComparison.OrdinalIgnoreCase));

            if (existingCommand == null)
            {
                return null;
            }

            // Update only provided values
            if (newDescription != null) existingCommand.Name = newDescription;
            if (executable != null) existingCommand.Executable = executable;
            if (workingDirectory != null) existingCommand.Directory = workingDirectory;
            if (shell != null) existingCommand.Shell = shell;
            if (folderPath != null) existingCommand.Bookmark = NormalizeFolderPath(folderPath);
            existingCommand.UpdatedAt = DateTime.UtcNow;

            commands = commands.OrderBy(c => c.Name).ToList();

            StorageService.SaveList<ShellCommand>(CommandsFilePath, commands);

            return existingCommand;
        }

        /// <summary>
        /// Gets all commands.
        /// </summary>
        /// <returns>A list of all commands.</returns>
        public static List<ShellCommand> GetList()
        {
            return StorageService.LoadList<ShellCommand>(CommandsFilePath);
        }

        /// <summary>
        /// Deletes a command by matching executable and working directory.
        /// </summary>
        /// <param name="command">The command to delete.</param>
        /// <returns>True if the command was deleted, false if not found.</returns>
        public static bool Delete(ShellCommand command)
        {
            var commands = StorageService.LoadList<ShellCommand>(CommandsFilePath);

            var existingCommand = commands.FirstOrDefault(c =>
                c.Executable == command.Executable &&
                c.Directory == command.Directory);

            if (existingCommand == null)
            {
                return false;
            }

            commands.Remove(existingCommand);
            StorageService.SaveList<ShellCommand>(CommandsFilePath, commands);

            return true;
        }

        /// <summary>
        /// Normalises a folder path by trimming leading/trailing whitespace and slashes.
        /// Returns null if the result is empty.
        /// </summary>
        internal static string? NormalizeFolderPath(string? folderPath)
        {
            var normalized = FolderNavigationService.NormalizePath(folderPath);
            return string.IsNullOrEmpty(normalized) ? null : normalized;
        }
    }
}

