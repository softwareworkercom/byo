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

        public async Task<List<NuGetPackage>?> ListPackagesAsync(string query, int skip = 0, int take = int.MaxValue, bool prerelease = true)
        {
            try
            {
                var allPackages = new List<NuGetPackage>();
                var result =  await _searchApi.SearchPackagesAsync(query, skip, take, prerelease);
                allPackages.AddRange(result.Data);
                return allPackages;
            }
            catch (Exception)
            {
                return null;
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


        public async Task<NuGetPackage?> GetPackageAsync(string packageId)
        {
            var result = await ListPackagesAsync(packageId, 0, 1);
            return result?.FirstOrDefault(p => p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
