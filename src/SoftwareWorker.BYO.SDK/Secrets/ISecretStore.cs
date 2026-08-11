namespace SoftwareWorker.BYO.Core.Secrets
{
    public interface ISecretStore
    {
        void SetSecret(string key, string secret);
        string GetSecret(string key);
        void DeleteSecret(string key);
    }
}