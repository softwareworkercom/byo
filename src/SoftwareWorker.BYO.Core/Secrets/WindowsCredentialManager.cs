using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SoftwareWorker.BYO.Core.Secrets
{
    public class WindowsSecretStore : ISecretStore
    {
        private const int CRED_TYPE_GENERIC = 1;
        private const int CRED_MAX_CREDENTIAL_BLOB_SIZE = 5 * 512;
        private const string Pkcs8Prefix = "pkcs8:";
        private const byte BinaryPkcs8Marker = 0x01;

        #region P/Invoke (minimal + safe)

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            public string TargetName;
            public string Comment;
            public long LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredWrite(ref CREDENTIAL userCredential, int flags);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredRead(string target, int type, int flags, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CredFree(IntPtr buffer);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredDelete(string target, int type, int flags);

        #endregion

        #region Public API

        public void SetSecret(string key, string secret)
        {
            if (secret.StartsWith(Pkcs8Prefix, StringComparison.Ordinal))
            {
                var base64 = secret[Pkcs8Prefix.Length..];
                var pkcs8Bytes = Convert.FromBase64String(base64);

                // Store compact binary format to stay within CredentialBlob limits.
                var payload = new byte[1 + pkcs8Bytes.Length];
                payload[0] = BinaryPkcs8Marker;
                Buffer.BlockCopy(pkcs8Bytes, 0, payload, 1, pkcs8Bytes.Length);

                WriteCredential(key, payload);
                return;
            }

            // For non-key payloads, store UTF-8 directly.
            WriteCredential(key, Encoding.UTF8.GetBytes(secret));
        }

        public string GetSecret(string key)
        {
            if (!CredRead(key, CRED_TYPE_GENERIC, 0, out IntPtr ptr))
                return null;

            try
            {
                var cred = Marshal.PtrToStructure<CREDENTIAL>(ptr);

                byte[] payload = new byte[cred.CredentialBlobSize];
                Marshal.Copy(cred.CredentialBlob, payload, 0, payload.Length);

                if (payload.Length > 1 && payload[0] == BinaryPkcs8Marker)
                {
                    var keyBytes = new byte[payload.Length - 1];
                    Buffer.BlockCopy(payload, 1, keyBytes, 0, keyBytes.Length);
                    return $"{Pkcs8Prefix}{Convert.ToBase64String(keyBytes)}";
                }

                var text = Encoding.UTF8.GetString(payload);

                // Backward compatibility: previous versions stored Base64(DPAPI(ciphertext)).
                try
                {
                    var encrypted = Convert.FromBase64String(text);
                    var decrypted = ProtectedData.Unprotect(
                        encrypted,
                        optionalEntropy: null,
                        scope: DataProtectionScope.CurrentUser);

                    return Encoding.UTF8.GetString(decrypted);
                }
                catch
                {
                    return text;
                }
            }
            finally
            {
                CredFree(ptr);
            }
        }

        public void DeleteSecret(string key)
        {
            CredDelete(key, CRED_TYPE_GENERIC, 0);
        }

        #endregion

        #region Internal write

        private void WriteCredential(string key, byte[] payload)
        {
            if (payload.Length > CRED_MAX_CREDENTIAL_BLOB_SIZE)
            {
                throw new InvalidOperationException(
                    $"Secret payload too large for Windows Credential Manager ({payload.Length} bytes). Max is {CRED_MAX_CREDENTIAL_BLOB_SIZE} bytes.");
            }

            var credential = new CREDENTIAL
            {
                TargetName = key,
                Type = CRED_TYPE_GENERIC,
                Persist = 2, // LocalMachine
                UserName = "app",
                CredentialBlobSize = payload.Length,
                CredentialBlob = Marshal.AllocHGlobal(payload.Length)
            };

            try
            {
                Marshal.Copy(payload, 0, credential.CredentialBlob, payload.Length);

                if (!CredWrite(ref credential, 0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(credential.CredentialBlob);
            }
        }

        #endregion
    }
}