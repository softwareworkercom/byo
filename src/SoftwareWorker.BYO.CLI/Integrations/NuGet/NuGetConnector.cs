using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.CLI.Integrations.NuGet.Model;
using SoftwareWorker.BYO.SDK.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SoftwareWorker.BYO.CLI.Integrations.NuGet
{
    public class NuGetConnector
    {
        private INuGetAPI _searchApi;
        private INuGetAPI _registrationApi;

        public NuGetConnector(bool isVerbose)
        {
            var settings = new RestSettings
            {
                SerializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    // Explicitly configure a metadata resolver so deserialization works even when
                    // reflection-based serialization is disabled by default (e.g. trimmed/AOT publish).
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                }
            }; 

            _searchApi = RestService.For<INuGetAPI>("https://azuresearch-usnc.nuget.org", settings);
            _registrationApi = RestService.For<INuGetAPI>("https://api.nuget.org/v3/registration5-gz-semver2", settings);
        }

        public async Task<List<NuGetPackage>?> ListPackagesAsync(string query, int skip = 0, int take = int.MaxValue, bool prerelease = true)
        {
            try
            {
                var allPackages = new List<NuGetPackage>();
                var result = await _searchApi.SearchPackagesAsync(query, skip, take, prerelease);
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
