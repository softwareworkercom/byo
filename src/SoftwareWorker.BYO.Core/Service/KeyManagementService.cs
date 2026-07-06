using SoftwareWorker.BYO.Core.Secrets;
using System.Security.Cryptography;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public static class KeyManagementService
    {
        private const int RsaKeySize = 4096;
        private const string Pkcs8Prefix = "pkcs8:";

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
            var key = SecretStore.Instance.GetSecret("byo-key");
            if (string.IsNullOrEmpty(key))
            {
                Initialize();
                key = SecretStore.Instance.GetSecret("byo-key");
            }
            return key;
        }

        /// <summary>
        /// Saves the RSA key pair to the file. Automatically encrypts the key pair.
        /// </summary>
        public static void Save(string keyPair)
        {
            SecretStore.Instance.SetSecret("byo-key", keyPair);
        }

        /// <summary>
        /// Initializes the RSA key pair if it doesn't exist.
        /// </summary>
        public static void Initialize()
        {
            var rsaKeyPair = CreateEncryptionKey();
            Save(rsaKeyPair);
        }
    }
}
