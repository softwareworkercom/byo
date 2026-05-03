using Refit;
using SoftwareWorker.BYO.Integrations.Helpers;
using SoftwareWorker.BYO.Integrations.NuGet.Model;

namespace SoftwareWorker.BYO.Integrations.NuGet
{
    public class NuGetConnector
    {
        private INuGetAPI _searchApi;
        private INuGetAPI _registrationApi;

        public NuGetConnector(bool isVerbose)
        {
            var settings = RefitHelper.GetSettings(isVerbose, "NuGet");
            _searchApi = RestService.For<INuGetAPI>("https://azuresearch-usnc.nuget.org", settings);
            _registrationApi = RestService.For<INuGetAPI>("https://api.nuget.org/v3/registration5-gz-semver2", settings);
        }

        public async Task<NuGetSearchResult> SearchPackagesAsync(string query, int skip = 0, int take = 20, bool prerelease = false)
        {
            try
            {
                return await _searchApi.SearchPackagesAsync(query, skip, take, prerelease);
            }
            catch (Exception)
            {
                return new NuGetSearchResult { TotalHits = 0, Data = Array.Empty<NuGetPackage>() };
            }
        }

        public async Task<NuGetRegistrationIndex?> GetPackageMetadataAsync(string packageId)
        {
            try
            {
                return await _registrationApi.GetPackageMetadataAsync(packageId.ToLowerInvariant());
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<NuGetPackage>> ListPackagesAsync(string query, int maxResults = 100)
        {
            var allPackages = new List<NuGetPackage>();
            var skip = 0;
            var take = 20;

            while (allPackages.Count < maxResults)
            {
                var result = await SearchPackagesAsync(query, skip, take);
                if (result.Data.Length == 0)
                {
                    break;
                }

                allPackages.AddRange(result.Data);

                if (allPackages.Count >= result.TotalHits)
                {
                    break;
                }

                skip += take;
            }

            return allPackages.Take(maxResults).ToList();
        }

        public async Task<NuGetPackage?> GetPackageAsync(string packageId)
        {
            var result = await SearchPackagesAsync(packageId, 0, 1);
            return result.Data.FirstOrDefault(p => p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
