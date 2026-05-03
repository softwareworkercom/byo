using Refit;
using SoftwareWorker.BYO.Integrations.HashiCorpVault.Model;

namespace SoftwareWorker.BYO.Integrations.HashiCorpVault
{
    /// <summary>
    /// https://developer.hashicorp.com/vault/api-docs
    /// </summary>
    internal interface IHashiCorpVaultAPI
    {
        [Get("/v1/sys/health")]
        Task<VaultHealthResponse> GetHealthAsync();

        [Get("/v1/{mountPath}/data/{secretPath}")]
        Task<VaultSecretResponse> ReadSecretAsync([AliasAs("mountPath")] string mountPath, [AliasAs("secretPath")] string secretPath);

        [Post("/v1/{mountPath}/data/{secretPath}")]
        Task<VaultSecretWriteResponse> WriteSecretAsync([AliasAs("mountPath")] string mountPath, [AliasAs("secretPath")] string secretPath, [Body] VaultSecretWriteRequest request);

        [Delete("/v1/{mountPath}/data/{secretPath}")]
        Task DeleteSecretAsync([AliasAs("mountPath")] string mountPath, [AliasAs("secretPath")] string secretPath);

        [Get("/v1/{mountPath}/metadata/{secretPath}")]
        Task<VaultSecretMetadataResponse> GetSecretMetadataAsync([AliasAs("mountPath")] string mountPath, [AliasAs("secretPath")] string secretPath);

        [Get("/v1/sys/mounts")]
        Task<VaultMountsResponse> ListMountsAsync();

        [Post("/v1/sys/mounts/{path}")]
        Task CreateMountAsync([AliasAs("path")] string path, [Body] VaultMountRequest request);

        [Delete("/v1/sys/mounts/{path}")]
        Task DeleteMountAsync([AliasAs("path")] string path);

        [Get("/v1/sys/policy")]
        Task<VaultPoliciesResponse> ListPoliciesAsync();

        [Get("/v1/sys/policy/{name}")]
        Task<VaultPolicyResponse> GetPolicyAsync([AliasAs("name")] string name);

        [Post("/v1/sys/policy/{name}")]
        Task CreatePolicyAsync([AliasAs("name")] string name, [Body] VaultPolicyRequest request);

        [Delete("/v1/sys/policy/{name}")]
        Task DeletePolicyAsync([AliasAs("name")] string name);

        [Post("/v1/auth/token/create")]
        Task<VaultTokenResponse> CreateTokenAsync([Body] VaultTokenCreateRequest request);

        [Post("/v1/auth/token/renew")]
        Task<VaultTokenResponse> RenewTokenAsync([Body] VaultTokenRenewRequest request);

        [Post("/v1/auth/token/revoke")]
        Task RevokeTokenAsync([Body] VaultTokenRevokeRequest request);

        [Get("/v1/auth/token/lookup-self")]
        Task<VaultTokenLookupResponse> LookupTokenAsync();
    }
}
