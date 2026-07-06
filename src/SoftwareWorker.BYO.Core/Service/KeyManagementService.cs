using SoftwareWorker.BYO.Core.Secrets;
using System.Security.Cryptography;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class KeyManagementService
    {
        private const int RsaKeySize = 4096;
        private const string Pkcs8Prefix = "pkcs8:";
        private const string DefaultSecretKeyName = "byo-key";

        public static string SecretKeyName { get; set; } = DefaultSecretKeyName;

        public static string CreateEncryptionKey()
        {
            using var rsa = RSA.Create(RsaKeySize);
            var privateKey = rsa.ExportPkcs8PrivateKey();
            return $"{Pkcs8Prefix}{Convert.ToBase64String(privateKey)}";
        }

        /// <summary>
        /// Gets the RSA key pair from the file. Returns decrypted key pair.
        /// </summary>
        public static string? Get()
        {
            var key = TryGetSecretFromStore();

            if (string.IsNullOrWhiteSpace(key))
            {
                key = ReadFallbackKey();
            }

            if (string.IsNullOrEmpty(key))
            {
                Initialize();
                key = TryGetSecretFromStore();

                if (string.IsNullOrWhiteSpace(key))
                {
                    key = ReadFallbackKey();
                }
            }

            return key;
        }

        /// <summary>
        /// Saves the RSA key pair to the file. Automatically encrypts the key pair.
        /// </summary>
        public static void Save(string keyPair)
        {
            if (!TrySaveSecretToStore(keyPair))
            {
                SaveFallbackKey(keyPair);
            }
        }

        /// <summary>
        /// Initializes the RSA key pair if it doesn't exist.
        /// </summary>
        public static void Initialize()
        {
            var rsaKeyPair = CreateEncryptionKey();
            Save(rsaKeyPair);
        }

        private static string? TryGetSecretFromStore()
        {
            try
            {
                return SecretStore.Instance.GetSecret(SecretKeyName);
            }
            catch
            {
                return null;
            }
        }

        private static bool TrySaveSecretToStore(string keyPair)
        {
            try
            {
                SecretStore.Instance.SetSecret(SecretKeyName, keyPair);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? ReadFallbackKey()
        {
            var fallbackPath = GetFallbackKeyPath();
            if (!File.Exists(fallbackPath))
            {
                return null;
            }

            var key = File.ReadAllText(fallbackPath);
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }

        private static void SaveFallbackKey(string keyPair)
        {
            var fallbackPath = GetFallbackKeyPath();
            var directory = Path.GetDirectoryName(fallbackPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fallbackPath, keyPair);
        }

        private static string GetFallbackKeyPath()
        {
            var settingsPath = SettingsService.SettingsFilePath;
            var directory = Path.GetDirectoryName(settingsPath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = AppContext.BaseDirectory;
            }

            return Path.Combine(directory, GetFallbackKeyFileName());
        }

        private static string GetFallbackKeyFileName()
        {
            var keyName = SecretKeyName;
            if (string.IsNullOrWhiteSpace(keyName))
            {
                keyName = DefaultSecretKeyName;
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitizedChars = new char[keyName.Length];
            for (var i = 0; i < keyName.Length; i++)
            {
                var currentChar = keyName[i];
                sanitizedChars[i] = Array.IndexOf(invalidChars, currentChar) >= 0 ? '_' : currentChar;
            }

            var sanitized = new string(sanitizedChars);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = DefaultSecretKeyName;
            }

            return $"{sanitized}.key";
        }
    }
}
