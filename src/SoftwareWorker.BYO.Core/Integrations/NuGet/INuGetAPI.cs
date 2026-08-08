using Refit;
using SoftwareWorker.BYO.Integrations.NuGet.Model;

namespace SoftwareWorker.BYO.Integrations.NuGet
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/nuget/api/overview
    /// </summary>
    internal interface INuGetAPI
    {
        [Get("/query?q={query}&skip={skip}&take={take}&prerelease={prerelease}")]
        Task<NuGetSearchResult> SearchPackagesAsync([AliasAs("query")] string query, [AliasAs("skip")] int skip = 0, [AliasAs("take")] int take = 20, [AliasAs("prerelease")] bool prerelease = false);

        [Get("/{packageId}/index.json")]
        Task<NuGetRegistrationIndex> GetPackageMetadataAsync([AliasAs("packageId")] string packageId);
    }
}
