using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.Core.Storage;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class WorkflowService
    {
        /// <summary>
        /// Creates a new workflow and adds it to storage.
        /// </summary>
        /// <param name="name">The name of the workflow.</param>
        /// <param name="description">Optional description of the workflow.</param>
        /// <param name="steps">The list of steps to include in the workflow.</param>
        /// <param name="folderPath">Optional hierarchical folder path (e.g. "DevOps/Deploy").</param>
        /// <returns>The created workflow.</returns>
        public static Workflow Create(string name, string? description, List<WorkflowStep> steps, string? folderPath = null)
        {
            var workflows = StorageService.LoadList<Workflow>(SystemConstants.STORAGE_WORKFLOWS_FILE);

            // Check for duplicate names
            if (workflows.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A workflow with the name '{name}' already exists.");
            }

            var workflow = new Workflow
            {
                Name = name,
                Description = description,
                Steps = steps,
                FolderPath = NormalizeFolderPath(folderPath),
                CreatedAt = DateTime.UtcNow
            };

            workflows.Add(workflow);
            workflows = [.. workflows.OrderBy(r => r.Name)];

            StorageService.SaveList(SystemConstants.STORAGE_WORKFLOWS_FILE, workflows);

            return workflow;
        }

        /// <summary>
        /// Gets all workflows.
        /// </summary>
        /// <returns>A list of all workflows.</returns>
        public static List<Workflow> GetList()
        {
            return StorageService.LoadList<Workflow>(SystemConstants.STORAGE_WORKFLOWS_FILE);
        }

        /// <summary>
        /// Gets a workflow by name.
        /// </summary>
        /// <param name="name">The name of the workflow to find.</param>
        /// <returns>The workflow if found, null otherwise.</returns>
        public static Workflow? GetByName(string name)
        {
            var workflows = StorageService.LoadList<Workflow>(SystemConstants.STORAGE_WORKFLOWS_FILE);
            return workflows.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Updates an existing workflow.
        /// </summary>
        /// <param name="name">The name of the workflow to update.</param>
        /// <param name="newName">Optional new name.</param>
        /// <param name="description">Optional new description.</param>
        /// <param name="steps">Optional new list of steps.</param>
        /// <param name="folderPath">Optional new folder path.</param>
        /// <returns>The updated workflow if found, null otherwise.</returns>
        public static Workflow? Update(
            string name,
            string? newName = null,
            string? description = null,
            List<WorkflowStep>? steps = null,
            string? folderPath = null)
        {
            var workflows = StorageService.LoadList<Workflow>(SystemConstants.STORAGE_WORKFLOWS_FILE);
            var existingWorkflow = workflows.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingWorkflow == null)
            {
                return null;
            }

            // Update only provided values
            if (newName != null) existingWorkflow.Name = newName;
            if (description != null) existingWorkflow.Description = description;
            if (steps != null) existingWorkflow.Steps = steps;
            if (folderPath != null) existingWorkflow.FolderPath = NormalizeFolderPath(folderPath);
            existingWorkflow.UpdatedAt = DateTime.UtcNow;

            StorageService.SaveList(SystemConstants.STORAGE_WORKFLOWS_FILE, workflows);

            // Re-read to get the properly ordered list
            workflows = StorageService.LoadList<Workflow>(SystemConstants.STORAGE_WORKFLOWS_FILE);
            workflows = [.. workflows.OrderBy(r => r.Name)];
            StorageService.SaveList(SystemConstants.STORAGE_WORKFLOWS_FILE, workflows);

            return existingWorkflow;
        }

        /// <summary>
        /// Deletes a workflow by name.
        /// </summary>
        /// <param name="name">The name of the workflow to delete.</param>
        /// <returns>True if the workflow was deleted, false if not found.</returns>
        public static bool Delete(string name)
        {
            var workflows = StorageService.LoadList<Workflow>(SystemConstants.STORAGE_WORKFLOWS_FILE);

            var existingWorkflow = workflows.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingWorkflow == null)
            {
                return false;
            }

            workflows.Remove(existingWorkflow);
            StorageService.SaveList(SystemConstants.STORAGE_WORKFLOWS_FILE, workflows);

            return true;
        }

        /// <summary>
        /// Deletes a workflow.
        /// </summary>
        /// <param name="workflow">The workflow to delete.</param>
        /// <returns>True if the workflow was deleted, false if not found.</returns>
        public static bool Delete(Workflow workflow)
        {
            return Delete(workflow.Name);
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
