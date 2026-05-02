using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.Core.Storage;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class RunbookService
    {
        /// <summary>
        /// Creates a new runbook and adds it to storage.
        /// </summary>
        /// <param name="name">The name of the runbook.</param>
        /// <param name="description">Optional description of the runbook.</param>
        /// <param name="commands">The list of commands to include in the runbook.</param>
        /// <returns>The created runbook.</returns>
        public static Runbook Create(string name, string? description, List<ShellCommand> commands)
        {
            var runbooks = StorageService.LoadList<Runbook>(SystemConstants.STORAGE_RUNBOOKS_FILE);

            var runbook = new Runbook
            {
                Name = name,
                Description = description,
                Commands = commands,
                CreatedAt = DateTime.UtcNow
            };

            runbooks.Add(runbook);
            runbooks = [.. runbooks.OrderBy(r => r.Name)];

            StorageService.SaveList(SystemConstants.STORAGE_RUNBOOKS_FILE, runbooks);

            return runbook;
        }

        /// <summary>
        /// Gets all runbooks.
        /// </summary>
        /// <returns>A list of all runbooks.</returns>
        public static List<Runbook> GetList()
        {
            return StorageService.LoadList<Runbook>(SystemConstants.STORAGE_RUNBOOKS_FILE);
        }

        /// <summary>
        /// Gets a runbook by name.
        /// </summary>
        /// <param name="name">The name of the runbook to find.</param>
        /// <returns>The runbook if found, null otherwise.</returns>
        public static Runbook? GetByName(string name)
        {
            var runbooks = StorageService.LoadList<Runbook>(SystemConstants.STORAGE_RUNBOOKS_FILE);
            return runbooks.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Updates an existing runbook.
        /// </summary>
        /// <param name="name">The name of the runbook to update.</param>
        /// <param name="newName">Optional new name.</param>
        /// <param name="description">Optional new description.</param>
        /// <param name="commands">Optional new list of commands.</param>
        /// <returns>The updated runbook if found, null otherwise.</returns>
        public static Runbook? Update(
            string name,
            string? newName = null,
            string? description = null,
            List<ShellCommand>? commands = null)
        {
            var runbooks = StorageService.LoadList<Runbook>(SystemConstants.STORAGE_RUNBOOKS_FILE);
            var existingRunbook = runbooks.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingRunbook == null)
            {
                return null;
            }

            // Update only provided values
            if (newName != null) existingRunbook.Name = newName;
            if (description != null) existingRunbook.Description = description;
            if (commands != null) existingRunbook.Commands = commands;
            existingRunbook.UpdatedAt = DateTime.UtcNow;

            runbooks = [.. runbooks.OrderBy(r => r.Name)];

            StorageService.SaveList(SystemConstants.STORAGE_RUNBOOKS_FILE, runbooks);

            return existingRunbook;
        }

        /// <summary>
        /// Deletes a runbook by name.
        /// </summary>
        /// <param name="name">The name of the runbook to delete.</param>
        /// <returns>True if the runbook was deleted, false if not found.</returns>
        public static bool Delete(string name)
        {
            var runbooks = StorageService.LoadList<Runbook>(SystemConstants.STORAGE_RUNBOOKS_FILE);

            var existingRunbook = runbooks.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingRunbook == null)
            {
                return false;
            }

            runbooks.Remove(existingRunbook);
            StorageService.SaveList(SystemConstants.STORAGE_RUNBOOKS_FILE, runbooks);

            return true;
        }

        /// <summary>
        /// Deletes a runbook.
        /// </summary>
        /// <param name="runbook">The runbook to delete.</param>
        /// <returns>True if the runbook was deleted, false if not found.</returns>
        public static bool Delete(Runbook runbook)
        {
            return Delete(runbook.Name);
        }
    }
}
