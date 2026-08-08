using Refit;
using SoftwareWorker.BYO.Integrations.Helpers;
using SoftwareWorker.BYO.Integrations.Raindrop.Model;
using SoftwareWorker.BYO.Integrations.Raindrop.Model.Request;

namespace SoftwareWorker.BYO.Integrations.Raindrop
{
    public class RaindropConnector
    {
        private readonly IRaindropAPI _api;

        public RaindropConnector(string accessToken, bool isVerbose)
        {
            var settings = RefitHelper.GetSettings(isVerbose, "Raindrop");
            settings.AuthorizationHeaderValueGetter = (_, __) => ValueTask.FromResult($"Bearer {accessToken}");
            _api = RestService.For<IRaindropAPI>("https://api.raindrop.io", settings);
        }

        public async Task<List<RaindropItem>?> ListRaindropsAsync(long collectionId = 0, int page = 0, int perpage = 25, string? search = null, string sort = "-created")
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListRaindropsAsync(collectionId, page, perpage, search, sort));
            return result?.Items;
        }

        public async Task<RaindropItem?> GetRaindropAsync(long id)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetRaindropAsync(id));
            return result?.Item;
        }

        public async Task<RaindropItem?> CreateRaindropAsync(string link, string? title = null, string? excerpt = null, List<string>? tags = null, long? collectionId = null, bool? important = null)
        {
            var request = new RaindropCreateRequest
            {
                Link = link,
                Title = title,
                Excerpt = excerpt,
                Tags = tags,
                Collection = collectionId.HasValue ? new RaindropCollectionRef { Id = collectionId.Value } : null,
                Important = important
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateRaindropAsync(request));
            return result?.Item;
        }

        public async Task<RaindropItem?> UpdateRaindropAsync(long id, string? title = null, string? excerpt = null, List<string>? tags = null, long? collectionId = null, bool? important = null)
        {
            var request = new RaindropCreateRequest
            {
                Link = string.Empty,
                Title = title,
                Excerpt = excerpt,
                Tags = tags,
                Collection = collectionId.HasValue ? new RaindropCollectionRef { Id = collectionId.Value } : null,
                Important = important
            };

            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateRaindropAsync(id, request));
            return result?.Item;
        }

        public async Task DeleteRaindropAsync(long id)
        {
            await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.DeleteRaindropAsync(id));
        }

        public async Task<List<RaindropCollection>?> ListCollectionsAsync(bool includeChildren = false)
        {
            if (includeChildren)
            {
                var root = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListRootCollectionsAsync());
                var children = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListChildCollectionsAsync());

                var all = new List<RaindropCollection>();
                if (root?.Items != null) all.AddRange(root.Items);
                if (children?.Items != null) all.AddRange(children.Items);
                return all;
            }
            else
            {
                var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListRootCollectionsAsync());
                return result?.Items;
            }
        }

        public async Task<List<RaindropTag>?> ListTagsAsync(long collectionId = 0)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.ListTagsAsync(collectionId));
            return result?.Items;
        }
    }
}
