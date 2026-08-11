using Refit;
using SoftwareWorker.BYO.Integrations.Raindrop.Model.Request;
using SoftwareWorker.BYO.Integrations.Raindrop.Model.Response;

namespace SoftwareWorker.BYO.Integrations.Raindrop
{
    /// <summary>
    /// Raindrop.io REST API v1 - https://developer.raindrop.io/
    /// </summary>
    internal interface IRaindropAPI
    {
        [Get("/rest/v1/raindrops/{collectionId}")]
        Task<RaindropListResponse> ListRaindropsAsync([AliasAs("collectionId")] long collectionId, [Query] int page = 0, [Query] int perpage = 25, [Query] string? search = null, [Query] string sort = "-created");

        [Get("/rest/v1/raindrop/{id}")]
        Task<RaindropSingleResponse> GetRaindropAsync([AliasAs("id")] long id);

        [Post("/rest/v1/raindrop")]
        Task<RaindropSingleResponse> CreateRaindropAsync([Body] RaindropCreateRequest request);

        [Put("/rest/v1/raindrop/{id}")]
        Task<RaindropSingleResponse> UpdateRaindropAsync([AliasAs("id")] long id, [Body] RaindropCreateRequest request);

        [Delete("/rest/v1/raindrop/{id}")]
        Task DeleteRaindropAsync([AliasAs("id")] long id);

        [Get("/rest/v1/collections")]
        Task<RaindropCollectionListResponse> ListRootCollectionsAsync();

        [Get("/rest/v1/collections/childrens")]
        Task<RaindropCollectionListResponse> ListChildCollectionsAsync();

        [Get("/rest/v1/collection/{id}")]
        Task<RaindropCollectionListResponse> GetCollectionAsync([AliasAs("id")] long id);

        [Get("/rest/v1/tags/{collectionId}")]
        Task<RaindropTagListResponse> ListTagsAsync([AliasAs("collectionId")] long collectionId = 0);
    }
}
