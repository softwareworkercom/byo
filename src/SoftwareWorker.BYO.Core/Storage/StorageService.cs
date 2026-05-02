using SoftwareWorker.BYO.CLI.Core.Helpers;
using System.Text.Json;

namespace SoftwareWorker.BYO.Core.Storage
{
    public class StorageService
    {
        public static Dictionary<string, string> LoadDictionary(string filePath)
        {
            var content = File.Exists(filePath) ? FileHelper.ReadFile(filePath) : string.Empty;
            return string.IsNullOrEmpty(content)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(content) ?? new Dictionary<string, string>();
        }

        public static List<T> LoadList<T>(string filePath)
        {
            var content = File.Exists(filePath) ? FileHelper.ReadFile(filePath) : string.Empty;
            return string.IsNullOrEmpty(content)
                ? new List<T>()
                : JsonSerializer.Deserialize<List<T>>(content) ?? new List<T>();
        }

        public static void SaveDictionary(string filePath, Dictionary<string, string> data)
        {
            var content = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            FileHelper.SaveFile(filePath, content);
        }

        public static void SaveList<T>(string filePath, List<T> data)
        {
            var content = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            FileHelper.SaveFile(filePath, content);
        }
    }
}
