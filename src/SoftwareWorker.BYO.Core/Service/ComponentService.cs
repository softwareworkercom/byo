using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.Core.Storage;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    /// <summary>
    /// Service for managing components.
    /// </summary>
    public static class ComponentService
    {
        /// <summary>
        /// Creates a new component and adds it to storage.
        /// </summary>
        /// <param name="name">The name of the component.</param>
        /// <param name="status">Optional status of the component.</param>
        /// <param name="repositoryName">Optional repository name.</param>
        /// <param name="defaultBranch">Optional default branch name.</param>
        /// <param name="ssh">Optional SSH connection string.</param>
        /// <param name="logs">Optional logs path or URL.</param>
        /// <param name="release">Optional release version.</param>
        /// <param name="owner">Optional owner or team.</param>
        /// <returns>The created component.</returns>
        public static Component Create(
            string name,
            string? status = null,
            string? repositoryName = null,
            string? defaultBranch = null,
            string? ssh = null,
            string? logs = null,
            string? release = null,
            string? owner = null)
        {
            var components = StorageService.LoadList<Component>(SystemConstants.STORAGE_COMPONENTS_FILE);

            var component = new Component
            {
                Name = name,
                Repository = new ComponentRepository
                {
                    Name = repositoryName ?? string.Empty,
                    DefaultBranch = defaultBranch ?? string.Empty
                },
                Owner = owner
            };

            components.Add(component);
            components = [.. components.OrderBy(c => c.Name)];

            StorageService.SaveList(SystemConstants.STORAGE_COMPONENTS_FILE, components);

            return component;
        }

        /// <summary>
        /// Gets all components.
        /// </summary>
        /// <returns>A list of all components.</returns>
        public static List<Component> GetList()
        {
            return StorageService.LoadList<Component>(SystemConstants.STORAGE_COMPONENTS_FILE);
        }

        /// <summary>
        /// Gets a component by name.
        /// </summary>
        /// <param name="name">The name of the component to find.</param>
        /// <returns>The component if found, null otherwise.</returns>
        public static Component? GetByName(string name)
        {
            var components = StorageService.LoadList<Component>(SystemConstants.STORAGE_COMPONENTS_FILE);
            return components.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Updates an existing component.
        /// </summary>
        /// <param name="name">The name of the component to update.</param>
        /// <param name="repositoryName">Optional new repository name.</param>
        /// <param name="defaultBranch">Optional new default branch.</param>
        /// <param name="owner">Optional new owner.</param>
        /// <returns>The updated component if found, null otherwise.</returns>
        public static Component? Update(
            string name,
            string? repositoryName = null,
            string? defaultBranch = null,
            string? owner = null)
        {
            var components = StorageService.LoadList<Component>(SystemConstants.STORAGE_COMPONENTS_FILE);
            var existingComponent = components.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingComponent == null)
            {
                return null;
            }

            // Update only provided values
            if (repositoryName != null) existingComponent.Repository.Name = repositoryName;
            if (defaultBranch != null) existingComponent.Repository.DefaultBranch = defaultBranch;
            if (owner != null) existingComponent.Owner = owner;

            StorageService.SaveList(SystemConstants.STORAGE_COMPONENTS_FILE, components);

            return existingComponent;
        }

        /// <summary>
        /// Deletes a component by name.
        /// </summary>
        /// <param name="name">The name of the component to delete.</param>
        /// <returns>True if the component was deleted, false if not found.</returns>
        public static bool Delete(string name)
        {
            var components = StorageService.LoadList<Component>(SystemConstants.STORAGE_COMPONENTS_FILE);
            var existingComponent = components.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingComponent == null)
            {
                return false;
            }

            components.Remove(existingComponent);
            StorageService.SaveList(SystemConstants.STORAGE_COMPONENTS_FILE, components);

            return true;
        }

        /// <summary>
        /// Deletes a component.
        /// </summary>
        /// <param name="component">The component to delete.</param>
        /// <returns>True if the component was deleted, false if not found.</returns>
        public static bool Delete(Component component)
        {
            return Delete(component.Name);
        }
    }
}
