using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Model;
using SoftwareWorker.BYO.Core.Storage;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    /// <summary>
    /// Service for managing browser bookmarks.
    /// </summary>
    public static class BookmarkService
    {
        /// <summary>
        /// Creates a new bookmark and adds it to storage.
        /// </summary>
        /// <param name="name">The name of the bookmark.</param>
        /// <param name="url">The URL of the bookmark.</param>
        /// <param name="folder">Optional folder for organizing the bookmark.</param>
        /// <returns>The created bookmark.</returns>
        public static Bookmark Create(
            string name,
            string url,
            string? folder = null)
        {
            var bookmarks = StorageService.LoadList<Bookmark>(SystemConstants.STORAGE_BOOKMARKS_FILE);

            var bookmark = new Bookmark
            {
                Name = name,
                Url = url,
                Folder = folder,
                CreatedAt = DateTime.UtcNow
            };

            bookmarks.Add(bookmark);
            bookmarks = [.. bookmarks.OrderBy(b => b.Folder).ThenBy(b => b.Name)];

            StorageService.SaveList(SystemConstants.STORAGE_BOOKMARKS_FILE, bookmarks);

            return bookmark;
        }

        /// <summary>
        /// Gets all bookmarks.
        /// </summary>
        /// <returns>A list of all bookmarks.</returns>
        public static List<Bookmark> GetList()
        {
            return StorageService.LoadList<Bookmark>(SystemConstants.STORAGE_BOOKMARKS_FILE);
        }

        /// <summary>
        /// Gets bookmarks by folder.
        /// </summary>
        /// <param name="folder">The folder to filter by.</param>
        /// <returns>A list of bookmarks in the specified folder.</returns>
        public static List<Bookmark> GetByFolder(string folder)
        {
            var bookmarks = StorageService.LoadList<Bookmark>(SystemConstants.STORAGE_BOOKMARKS_FILE);
            return bookmarks.Where(b => b.Folder != null && b.Folder.Equals(folder, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Gets a bookmark by name.
        /// </summary>
        /// <param name="name">The name of the bookmark to find.</param>
        /// <returns>The bookmark if found, null otherwise.</returns>
        public static Bookmark? GetByName(string name)
        {
            var bookmarks = StorageService.LoadList<Bookmark>(SystemConstants.STORAGE_BOOKMARKS_FILE);
            return bookmarks.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Updates an existing bookmark.
        /// </summary>
        /// <param name="name">The name of the bookmark to update.</param>
        /// <param name="url">Optional new URL.</param>
        /// <param name="folder">Optional new folder.</param>
        /// <returns>The updated bookmark if found, null otherwise.</returns>
        public static Bookmark? Update(
            string name,
            string? url = null,
            string? folder = null)
        {
            var bookmarks = StorageService.LoadList<Bookmark>(SystemConstants.STORAGE_BOOKMARKS_FILE);
            var existingBookmark = bookmarks.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingBookmark == null)
            {
                return null;
            }

            // Update only provided values
            if (url != null) existingBookmark.Url = url;
            if (folder != null) existingBookmark.Folder = folder;
            existingBookmark.UpdatedAt = DateTime.UtcNow;

            bookmarks = [.. bookmarks.OrderBy(b => b.Folder).ThenBy(b => b.Name)];

            StorageService.SaveList(SystemConstants.STORAGE_BOOKMARKS_FILE, bookmarks);

            return existingBookmark;
        }

        /// <summary>
        /// Deletes a bookmark by name.
        /// </summary>
        /// <param name="name">The name of the bookmark to delete.</param>
        /// <returns>True if the bookmark was deleted, false if not found.</returns>
        public static bool Delete(string name)
        {
            var bookmarks = StorageService.LoadList<Bookmark>(SystemConstants.STORAGE_BOOKMARKS_FILE);
            var existingBookmark = bookmarks.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingBookmark == null)
            {
                return false;
            }

            bookmarks.Remove(existingBookmark);
            StorageService.SaveList(SystemConstants.STORAGE_BOOKMARKS_FILE, bookmarks);

            return true;
        }

        /// <summary>
        /// Deletes a bookmark.
        /// </summary>
        /// <param name="bookmark">The bookmark to delete.</param>
        /// <returns>True if the bookmark was deleted, false if not found.</returns>
        public static bool Delete(Bookmark bookmark)
        {
            return Delete(bookmark.Name);
        }
    }
}
