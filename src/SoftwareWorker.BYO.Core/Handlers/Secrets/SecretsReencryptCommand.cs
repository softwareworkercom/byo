using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Storage;

namespace SoftwareWorker.BYO.CLI.Core.Handlers.Secrets
{
    [TrunkCommand("secrets", "Encrypted secrets operations")]
    [BranchCommand("reencrypt", "Re-encrypt all secrets with a new key")]
    public class SecretsReencryptCommand : BaseCommandHandler
    {
        public override async Task ExecuteAsync()
        {
            //decrypt vault entries
            var decryptedItems = new Dictionary<string, string>();
            var currentSecrets = SecretsService.GetList();
            if (currentSecrets is not null)
            {
                foreach (var item in currentSecrets)
                {
                    // GetList already returns decrypted values, so use item.Value directly
                    decryptedItems.Add(item.Key, item.Value);
                }
            }

            //Generate new encryption key
            var newRSAKeyPair = KeyManagementService.Generate();

            // Update RSA key FIRST so it's available for the new encryptions
            KeyManagementService.Save(newRSAKeyPair);

            // Re-encrypt vault entries directly to avoid double encryption
            var secrets = StorageService.LoadDictionary(SystemConstants.STORAGE_SECRETS_FILE);
            foreach (var item in decryptedItems)
            {
                // Encrypt with the new key (now stored) and update secrets directly
                var newEncryptedValue = EncryptionService.EncryptVaultEntry(item.Value);
                secrets[item.Key] = newEncryptedValue;
            }
            StorageService.SaveDictionary(SystemConstants.STORAGE_SECRETS_FILE, secrets);

            UserInterfaceService.ShowGreen("Secrets re-encrypted with new key.");
        }
    }
}
