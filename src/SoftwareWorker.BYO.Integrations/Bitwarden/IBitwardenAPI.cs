using Refit;
using SoftwareWorker.BYO.Integrations.Bitwarden.Model;

namespace SoftwareWorker.BYO.Integrations.Bitwarden
{
    /// <summary>
    /// Bitwarden Secrets Manager API - https://bitwarden.com/help/secrets-manager-sdk/
    /// </summary>
    internal interface IBitwardenAPI
    {
        // Secrets endpoints
        [Get("/api/secrets")]
        Task<BitwardenSecretsListResponse> ListSecretsAsync([Query] string? organizationId = null);

        [Get("/api/secrets/{id}")]
        Task<BitwardenSecretResponse> GetSecretAsync([AliasAs("id")] string secretId);

        [Post("/api/secrets/{organizationId}")]
        Task<BitwardenSecretResponse> CreateSecretAsync(
            [AliasAs("organizationId")] string organizationId,
            [Body] BitwardenSecretCreateRequest request);

        [Put("/api/secrets/{id}")]
        Task<BitwardenSecretResponse> UpdateSecretAsync(
            [AliasAs("id")] string secretId,
            [Body] BitwardenSecretUpdateRequest request);

        [Delete("/api/secrets/{id}")]
        Task DeleteSecretAsync([AliasAs("id")] string secretId);

        // Projects endpoints
        [Get("/api/projects")]
        Task<BitwardenProjectsListResponse> ListProjectsAsync([Query] string? organizationId = null);

        [Get("/api/projects/{id}")]
        Task<BitwardenProject> GetProjectAsync([AliasAs("id")] string projectId);
    }
}
