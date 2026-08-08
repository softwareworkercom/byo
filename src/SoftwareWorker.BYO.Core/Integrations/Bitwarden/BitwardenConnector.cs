using Refit;
using SoftwareWorker.BYO.Integrations.Bitwarden.Model;
using SoftwareWorker.BYO.Integrations.Helpers;

namespace SoftwareWorker.BYO.Integrations.Bitwarden
{
    public class BitwardenConnector
    {
        private readonly IBitwardenAPI _api;

        public BitwardenConnector(string apiUrl, string accessToken, bool isVerbose = false)
        {
            var settings = RefitHelper.GetSettings(isVerbose, "Bitwarden");
            settings.AuthorizationHeaderValueGetter = (_, __) => ValueTask.FromResult(accessToken);
            _api = RestService.For<IBitwardenAPI>(apiUrl, settings);
        }

        public async Task<List<BitwardenSecret>?> ListSecretsAsync(string? organizationId = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListSecretsAsync(organizationId));
            return result?.Data;
        }

        public async Task<BitwardenSecretResponse?> GetSecretAsync(string secretId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetSecretAsync(secretId));
            return result;
        }

        public async Task<BitwardenSecretResponse?> CreateSecretAsync(
            string organizationId,
            string key,
            string value,
            string? note = null,
            List<string>? projectIds = null)
        {
            var request = new BitwardenSecretCreateRequest
            {
                Key = key,
                Value = value,
                Note = note,
                ProjectIds = projectIds
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateSecretAsync(organizationId, request));
            return result;
        }

        public async Task<BitwardenSecretResponse?> UpdateSecretAsync(
            string secretId,
            string? key = null,
            string? value = null,
            string? note = null,
            List<string>? projectIds = null)
        {
            var request = new BitwardenSecretUpdateRequest
            {
                Key = key,
                Value = value,
                Note = note,
                ProjectIds = projectIds
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateSecretAsync(secretId, request));
            return result;
        }

        public async Task<bool> DeleteSecretAsync(string secretId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => { await _api.DeleteSecretAsync(secretId); return new object(); });
            return result != null;
        }

        public async Task<List<BitwardenProject>?> ListProjectsAsync(string? organizationId = null)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListProjectsAsync(organizationId));
            return result?.Data;
        }

        public async Task<BitwardenProject?> GetProjectAsync(string projectId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetProjectAsync(projectId));
            return result;
        }
    }
}
