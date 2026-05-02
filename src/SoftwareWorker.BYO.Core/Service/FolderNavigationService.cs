namespace SoftwareWorker.BYO.CLI.Core.Service
{
    /// <summary>
    /// Provides hierarchical folder navigation for selecting commands and workflows.
    /// Items are organised using a slash-separated folder path (e.g. "DevOps/Deploy").
    /// </summary>
    public static class FolderNavigationService
    {
        private const string BackOption = "Back";
        private const string FolderPrefix = "/";

        /// <summary>
        /// Interactively navigates the folder hierarchy and returns the selected item.
        /// Returns null if there are no items or the user cannot make a selection.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <param name="items">All items to navigate.</param>
        /// <param name="getFolderPath">Selector for the item's folder path.</param>
        /// <param name="getDisplayName">Selector for the item's display name shown in the prompt.</param>
        /// <param name="selectionTitle">Title shown in the selection prompt.</param>
        /// <returns>The selected item, or null if selection is not possible.</returns>
        public static T? NavigateAndSelect<T>(
            List<T> items,
            Func<T, string?> getFolderPath,
            Func<T, string> getDisplayName,
            string selectionTitle = "item") where T : class
        {
            if (items.Count == 0)
            {
                return null;
            }

            var currentPath = string.Empty;

            while (true)
            {
                // Items directly inside the current folder level
                var itemsAtLevel = items
                    .Where(i => NormalizePath(getFolderPath(i)) == currentPath)
                    .OrderBy(getDisplayName)
                    .ToList();

                // Immediate sub-folders at the current level
                var subFolders = items
                    .Select(i => GetImmediateSubFolder(getFolderPath(i), currentPath))
                    .Where(f => f != null)
                    .Distinct()
                    .OrderBy(f => f)
                    .ToList();

                var options = new List<string>();

                if (!string.IsNullOrEmpty(currentPath))
                {
                    options.Add(BackOption);
                }

                foreach (var folder in subFolders)
                {
                    options.Add($"{FolderPrefix}{folder}/");
                }

                foreach (var item in itemsAtLevel)
                {
                    options.Add(getDisplayName(item));
                }

                if (options.Count == 0)
                {
                    return null;
                }

                var pathDisplay = string.IsNullOrEmpty(currentPath) ? "/" : $"/{currentPath}/";
                var selected = UserInterfaceService.SelectSingleItem($"{selectionTitle} [grey]{pathDisplay}[/]", options);

                if (selected == BackOption)
                {
                    var lastSlash = currentPath.LastIndexOf('/');
                    currentPath = lastSlash >= 0 ? currentPath[..lastSlash] : string.Empty;
                    continue;
                }

                if (selected.StartsWith(FolderPrefix))
                {
                    // Strip prefix ("📁 ") and trailing slash
                    var folderName = selected[FolderPrefix.Length..^1];
                    currentPath = string.IsNullOrEmpty(currentPath)
                        ? folderName
                        : $"{currentPath}/{folderName}";
                    continue;
                }

                return items.FirstOrDefault(i =>
                    NormalizePath(getFolderPath(i)) == currentPath &&
                    getDisplayName(i) == selected);
            }
        }

        /// <summary>
        /// Returns all unique folder paths present in the given item collection,
        /// including intermediate ancestor paths.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <param name="items">All items.</param>
        /// <param name="getFolderPath">Selector for the item's folder path.</param>
        /// <returns>Sorted list of unique folder paths.</returns>
        public static List<string> GetAllFolderPaths<T>(
            List<T> items,
            Func<T, string?> getFolderPath)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var path = NormalizePath(getFolderPath(item));
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                // Add the path and all ancestor paths
                var parts = path.Split('/');
                for (var i = 1; i <= parts.Length; i++)
                {
                    paths.Add(string.Join('/', parts[..i]));
                }
            }

            return [.. paths.OrderBy(p => p)];
        }

        /// <summary>
        /// Normalises a folder path: trims whitespace and leading/trailing slashes.
        /// </summary>
        public static string NormalizePath(string? path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim().Trim('/');
        }

        /// <summary>
        /// Returns the immediate sub-folder name directly under <paramref name="currentPath"/>,
        /// or null if the item is not in a deeper level.
        /// </summary>
        private static string? GetImmediateSubFolder(string? itemPath, string currentPath)
        {
            var normalized = NormalizePath(itemPath);
            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            var prefix = string.IsNullOrEmpty(currentPath) ? string.Empty : currentPath + "/";

            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var remaining = normalized[prefix.Length..];
            if (string.IsNullOrEmpty(remaining))
            {
                return null;
            }

            var slashIndex = remaining.IndexOf('/');
            return slashIndex >= 0 ? remaining[..slashIndex] : remaining;
        }
    }
}
