using SoftwareWorker.BYO.CLI.Core.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public class EncryptionService
    {
        // AES-GCM constants
        private const int SaltSize = 32;        // 256-bit salt for PBKDF2
        private const int NonceSize = 12;       // 96-bit nonce for AES-GCM (recommended size)
        private const int TagSize = 16;         // 128-bit authentication tag
        private const int KeySize = 32;         // 256-bit key for AES-256
        private const int Pbkdf2Iterations = 600000; // OWASP recommended iterations for SHA-256

        private static readonly byte[] _masterSecret = GenerateMachineSpecificSecret();

        /// <summary>
        /// Generates a machine-specific secret using hardware identifiers.
        /// This is used as input for key derivation, not directly as an encryption key.
        /// Uses cross-platform runtime information instead of WMI.
        /// </summary>
        private static byte[] GenerateMachineSpecificSecret()
        {
            string machineId = MachineIdentifierHelper.GetMachineIdentifier();
            // Use SHA-512 to get more entropy from the machine identifier
            return SHA512.HashData(Encoding.UTF8.GetBytes(machineId));
        }

        /// <summary>
        /// Derives an encryption key using PBKDF2 with the machine secret and a random salt.
        /// </summary>
        private static byte[] DeriveKey(byte[] salt)
        {
            // Use PBKDF2 with SHA-256 for secure key derivation
            return Rfc2898DeriveBytes.Pbkdf2(
                _masterSecret,
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                KeySize);
        }



        /// <summary>
        /// Encrypts a vault entry using RSA-OAEP with SHA-256.
        /// For larger data, uses hybrid encryption (AES-GCM + RSA).
        /// </summary>
        public static string EncryptVaultEntry(string vaultEntryValue, string? newRSAKeyPair = null)
        {
            if (string.IsNullOrEmpty(vaultEntryValue))
                throw new ArgumentNullException(nameof(vaultEntryValue));

            var rsaKeyPair = newRSAKeyPair ?? KeyManagementService.Get();

            byte[] plaintext = Encoding.UTF8.GetBytes(vaultEntryValue);

            using (RSA rsa = RSA.Create())
            {
                rsa.FromXmlString(rsaKeyPair);

                // RSA-OAEP with SHA-256 has a maximum payload size of: KeySize/8 - 2*HashSize - 2
                // For 4096-bit key with SHA-256: 512 - 64 - 2 = 446 bytes
                int maxRsaPayload = (rsa.KeySize / 8) - (2 * 32) - 2;

                if (plaintext.Length <= maxRsaPayload)
                {
                    // Direct RSA encryption for small data
                    var encryptedData = rsa.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA256);
                    // Prefix with 0x00 to indicate direct RSA encryption
                    byte[] result = new byte[1 + encryptedData.Length];
                    result[0] = 0x00;
                    Buffer.BlockCopy(encryptedData, 0, result, 1, encryptedData.Length);
                    return Convert.ToBase64String(result);
                }
                else
                {
                    // Hybrid encryption for larger data
                    // Generate random AES key and encrypt data with AES-GCM
                    byte[] aesKey = RandomNumberGenerator.GetBytes(KeySize);
                    byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
                    byte[] ciphertext = new byte[plaintext.Length];
                    byte[] tag = new byte[TagSize];

                    try
                    {
                        using (AesGcm aesGcm = new AesGcm(aesKey, TagSize))
                        {
                            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
                        }

                        // Encrypt the AES key with RSA
                        byte[] encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);

                        // Format: [0x01][EncryptedKeyLength (2 bytes)][EncryptedKey][Nonce][Tag][Ciphertext]
                        int totalLength = 1 + 2 + encryptedAesKey.Length + NonceSize + TagSize + ciphertext.Length;
                        byte[] result = new byte[totalLength];
                        int offset = 0;

                        result[offset++] = 0x01; // Hybrid encryption marker

                        // Store encrypted key length (big-endian)
                        result[offset++] = (byte)(encryptedAesKey.Length >> 8);
                        result[offset++] = (byte)(encryptedAesKey.Length & 0xFF);

                        Buffer.BlockCopy(encryptedAesKey, 0, result, offset, encryptedAesKey.Length);
                        offset += encryptedAesKey.Length;

                        Buffer.BlockCopy(nonce, 0, result, offset, NonceSize);
                        offset += NonceSize;

                        Buffer.BlockCopy(tag, 0, result, offset, TagSize);
                        offset += TagSize;

                        Buffer.BlockCopy(ciphertext, 0, result, offset, ciphertext.Length);

                        return Convert.ToBase64String(result);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(aesKey);
                        CryptographicOperations.ZeroMemory(plaintext);
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts a vault entry. Automatically detects direct RSA or hybrid encryption.
        /// </summary>
        public static string DecryptVaultEntry(string vaultEntryValue)
        {
            if (string.IsNullOrEmpty(vaultEntryValue))
                throw new ArgumentNullException(nameof(vaultEntryValue));

            var rsaKeyPair = KeyManagementService.Get();

            byte[] encryptedData = Convert.FromBase64String(vaultEntryValue);

            if (encryptedData.Length < 1)
                throw new CryptographicException("Invalid encrypted data.");

            using (RSA rsa = RSA.Create())
            {
                rsa.FromXmlString(rsaKeyPair);

                byte encryptionType = encryptedData[0];

                if (encryptionType == 0x00)
                {
                    // Direct RSA decryption
                    byte[] rsaCiphertext = new byte[encryptedData.Length - 1];
                    Buffer.BlockCopy(encryptedData, 1, rsaCiphertext, 0, rsaCiphertext.Length);

                    var decryptedData = rsa.Decrypt(rsaCiphertext, RSAEncryptionPadding.OaepSHA256);
                    return Encoding.UTF8.GetString(decryptedData);
                }
                else if (encryptionType == 0x01)
                {
                    // Hybrid decryption
                    int offset = 1;

                    // Read encrypted key length
                    int encryptedKeyLength = (encryptedData[offset] << 8) | encryptedData[offset + 1];
                    offset += 2;

                    // Extract encrypted AES key
                    byte[] encryptedAesKey = new byte[encryptedKeyLength];
                    Buffer.BlockCopy(encryptedData, offset, encryptedAesKey, 0, encryptedKeyLength);
                    offset += encryptedKeyLength;

                    // Decrypt AES key using RSA
                    byte[] aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);

                    try
                    {
                        // Extract nonce
                        byte[] nonce = new byte[NonceSize];
                        Buffer.BlockCopy(encryptedData, offset, nonce, 0, NonceSize);
                        offset += NonceSize;

                        // Extract tag
                        byte[] tag = new byte[TagSize];
                        Buffer.BlockCopy(encryptedData, offset, tag, 0, TagSize);
                        offset += TagSize;

                        // Extract ciphertext
                        byte[] ciphertext = new byte[encryptedData.Length - offset];
                        Buffer.BlockCopy(encryptedData, offset, ciphertext, 0, ciphertext.Length);

                        // Decrypt with AES-GCM
                        byte[] plaintext = new byte[ciphertext.Length];
                        using (AesGcm aesGcm = new AesGcm(aesKey, TagSize))
                        {
                            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
                        }

                        return Encoding.UTF8.GetString(plaintext);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(aesKey);
                    }
                }
                else
                {
                    throw new CryptographicException($"Unknown encryption type: {encryptionType}");
                }
            }
        }

        /// <summary>
        /// Securely compares two byte arrays in constant time to prevent timing attacks.
        /// </summary>
        public static bool SecureCompare(byte[] a, byte[] b)
        {
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        /// <summary>
        /// Generates a cryptographically secure random string of the specified length.
        /// </summary>
        public static string GenerateSecureRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            byte[] randomBytes = RandomNumberGenerator.GetBytes(length);
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[randomBytes[i] % chars.Length];
            }

            return new string(result);
        }
    }
}
