namespace SoftwareWorker.BYO.SDK.Vault
{
    internal interface IVault
    {
        void SetSecret(string key, string secret);
        string GetSecret(string key);
        void DeleteSecret(string key);
    }
}