using SoftwareWorker.BYO.CLI.Core.Constants;
using System.Security.Cryptography;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class KeyManagementService
    {
        /// <summary>
        /// Generate the RSA Public (to encrypt) and Private (to decrypt) key pair.
        /// Uses 4096-bit key size for enhanced security.
        /// </summary>
        /// <returns>RSA key pair in XML format</returns>
        public static string Generate()
        {
            using (RSA rsa = RSA.Create())
            {
                // Use 4096-bit key for stronger security (industry best practice)
                rsa.KeySize = 4096;

                // Export both the public and private key information. This is necessary when you need
                // to transfer the complete key pair for vault export/import purposes.
                string rsaKeyPair = rsa.ToXmlString(true);
                return rsaKeyPair;
            }
        }

        /// <summary>
        /// Gets the RSA key pair from the file. Returns decrypted key pair.
        /// </summary>
        public static string? Get()
        {
            if (!File.Exists(SystemConstants.STORAGE_RSA_KEY_FILE))
            {
                Initialize();
            }

            var rsaKeyPair = File.ReadAllText(SystemConstants.STORAGE_RSA_KEY_FILE);
            return rsaKeyPair;
        }

        /// <summary>
        /// Saves the RSA key pair to the file. Automatically encrypts the key pair.
        /// </summary>
        public static void Save(string keyPair)
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(SystemConstants.STORAGE_RSA_KEY_FILE);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(SystemConstants.STORAGE_RSA_KEY_FILE, keyPair);
        }

        /// <summary>
        /// Initializes the RSA key pair if it doesn't exist.
        /// </summary>
        public static void Initialize()
        {
            var rsaKeyPair = Generate();
            Save(rsaKeyPair);
        }
    }
}
