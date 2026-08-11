using Refit;
using SoftwareWorker.BYO.Integrations.HashiCorpVault.Model;
using SoftwareWorker.BYO.Integrations.Helpers;

namespace SoftwareWorker.BYO.Integrations.HashiCorpVault
{
    public class HashiCorpVaultConnector
    {
        private readonly IHashiCorpVaultAPI _api;

        public HashiCorpVaultConnector(string vaultAddress, string token, bool isVerbose)
        {
            var settings = RefitHelper.GetSettings(isVerbose, "HashiCorpVault");
            settings.AuthorizationHeaderValueGetter = (_, __) => ValueTask.FromResult(token);
            _api = RestService.For<IHashiCorpVaultAPI>(vaultAddress, settings);
        }

        public async Task<VaultHealthResponse?> GetHealthAsync()
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetHealthAsync());
            return result;
        }

        public async Task<VaultSecretResponse?> ReadSecretAsync(string mountPath, string secretPath)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ReadSecretAsync(mountPath, secretPath));
            return result;
        }

        public async Task<VaultSecretWriteResponse?> WriteSecretAsync(string mountPath, string secretPath, Dictionary<string, object> data)
        {
            var request = new VaultSecretWriteRequest { Data = data };
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.WriteSecretAsync(mountPath, secretPath, request));
            return result;
        }

        public async Task<bool> DeleteSecretAsync(string mountPath, string secretPath)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.DeleteSecretAsync(mountPath, secretPath); return new object(); });
            return result != null;
        }

        public async Task<VaultSecretMetadataResponse?> GetSecretMetadataAsync(string mountPath, string secretPath)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetSecretMetadataAsync(mountPath, secretPath));
            return result;
        }

        public async Task<Dictionary<string, VaultMount>?> ListMountsAsync()
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListMountsAsync());
            return result?.Data;
        }

        public async Task<bool> CreateMountAsync(string path, string type, string description = "")
        {
            var request = new VaultMountRequest { Type = type, Description = description };
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.CreateMountAsync(path, request); return new object(); });
            return result != null;
        }

        public async Task<bool> DeleteMountAsync(string path)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.DeleteMountAsync(path); return new object(); });
            return result != null;
        }

        public async Task<List<string>?> ListPoliciesAsync()
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListPoliciesAsync());
            return result?.Policies?.ToList();
        }

        public async Task<VaultPolicyResponse?> GetPolicyAsync(string name)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetPolicyAsync(name));
            return result;
        }

        public async Task<bool> CreatePolicyAsync(string name, string policy)
        {
            var request = new VaultPolicyRequest { Policy = policy };
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.CreatePolicyAsync(name, request); return new object(); });
            return result != null;
        }

        public async Task<bool> DeletePolicyAsync(string name)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.DeletePolicyAsync(name); return new object(); });
            return result != null;
        }

        public async Task<VaultTokenResponse?> CreateTokenAsync(string? displayName = null, int? ttl = null, List<string>? policies = null)
        {
            var request = new VaultTokenCreateRequest
            {
                DisplayName = displayName,
                Ttl = ttl,
                Policies = policies
            };
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateTokenAsync(request));
            return result;
        }

        public async Task<VaultTokenResponse?> RenewTokenAsync(string token, int? increment = null)
        {
            var request = new VaultTokenRenewRequest { Token = token, Increment = increment };
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.RenewTokenAsync(request));
            return result;
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            var request = new VaultTokenRevokeRequest { Token = token };
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.RevokeTokenAsync(request); return new object(); });
            return result != null;
        }

        public async Task<VaultTokenLookupResponse?> LookupTokenAsync()
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.LookupTokenAsync());
            return result;
        }
    }
}
