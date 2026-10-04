using System.Text.RegularExpressions;

namespace SoftwareWorker.BYO.CLI.Shell
{
    /// <summary>
    /// Command history of the interactive shell, persisted one entry per line.
    /// </summary>
    internal sealed class ShellHistory
    {
        // Same idea as PSReadLine: lines that look like they carry a secret are kept for the session only.
        private static readonly Regex SensitivePattern = new(
            "password|passwd|secret|token|apikey|api-key|credential",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly List<string> _entries = [];
        private readonly HashSet<string> _sessionOnlyEntries = new(StringComparer.Ordinal);
        private readonly string? _filePath;
        private readonly int _maxEntries;

        public ShellHistory(string? filePath = null, int maxEntries = 1000)
        {
            _filePath = filePath;
            _maxEntries = maxEntries;
        }

        public IReadOnlyList<string> Entries => _entries;

        public static ShellHistory Load(string filePath, int maxEntries = 1000)
        {
            var history = new ShellHistory(filePath, maxEntries);

            try
            {
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath).Where(l => !string.IsNullOrWhiteSpace(l));
                    history._entries.AddRange(lines.TakeLast(maxEntries));
                }
            }
            catch
            {
                // An unreadable history file should not prevent the shell from starting.
            }

            return history;
        }

        public void Add(string entry)
        {
            entry = entry.Trim();
            if (entry.Length == 0)
            {
                return;
            }

            // Move repeated commands to the end instead of storing duplicates.
            _entries.Remove(entry);
            _entries.Add(entry);

            if (_entries.Count > _maxEntries)
            {
                _entries.RemoveRange(0, _entries.Count - _maxEntries);
            }

            if (SensitivePattern.IsMatch(entry))
            {
                _sessionOnlyEntries.Add(entry);
            }

            Save();
        }

        public void Clear()
        {
            _entries.Clear();
            _sessionOnlyEntries.Clear();
            Save();
        }

        /// <summary>
        /// Returns the most recent entry that extends <paramref name="prefix"/>, used for inline suggestions.
        /// </summary>
        public string? FindSuggestion(string prefix)
        {
            if (prefix.Length == 0)
            {
                return null;
            }

            for (var index = _entries.Count - 1; index >= 0; index--)
            {
                var entry = _entries[index];
                if (entry.Length > prefix.Length && entry.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_filePath))
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllLines(_filePath, _entries.Where(e => !_sessionOnlyEntries.Contains(e)));
            }
            catch
            {
                // History is a convenience; failing to save it must not interrupt the shell.
            }
        }
    }
}
