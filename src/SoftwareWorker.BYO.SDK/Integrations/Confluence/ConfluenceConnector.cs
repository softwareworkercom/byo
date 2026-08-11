using Refit;
using SoftwareWorker.BYO.Integrations.Confluence.Model;
using SoftwareWorker.BYO.Integrations.Helpers;
using System.Text;

namespace SoftwareWorker.BYO.Integrations.Confluence
{
    public class ConfluenceConnector
    {
        private readonly Dictionary<string, string> _headers;
        private readonly IConfluenceAPI _api;

        public ConfluenceConnector(string baseUrl, string user, string key, bool isVerbose)
        {
            var atlassianAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{key}"));

            _headers = new Dictionary<string, string> {
                                                            { "Authorization", $"Basic {atlassianAuth}"}
                                                      };

            RefitSettings settings = RefitHelper.GetSettings(isVerbose, "Confluence");
            _api = RestService.For<IConfluenceAPI>(baseUrl, settings);
        }

        public async Task<ConfluencePage> GetPageAsync(string pageId)
        {
            return await _api.GetPageAsync(_headers, pageId);
        }

        public async Task UpdatePageAsync(string pageId, string htmlContent, string comment)
        {
            ConfluencePage wikiPage = await _api.GetPageAsync(_headers, pageId);

            var requestBody = new ConfluencePageUpdateRequest()
            {
                Type = "page",
                Title = wikiPage.Title,
                Body = new ConfluencePageBody()
                {
                    Storage = new ConfluencePageBodyStorage()
                    {
                        Value = htmlContent,
                        Representation = "storage"
                    }
                },
                Version = new ConfluencePageVersion()
                {
                    Number = wikiPage.Version.Number + 1,
                    Message = comment
                }
            };
            _ = await _api.UpdatePageAsync(_headers, pageId, requestBody);
        }

        public async Task<ConfluencePage> CreatePageAsync(string spaceKey, string title, string htmlContent)
        {
            var request = new ConfluencePageCreateRequest
            {
                Type = "page",
                Title = title,
                Space = new ConfluencePageSpace { Key = spaceKey },
                Body = new ConfluencePageBody
                {
                    Storage = new ConfluencePageBodyStorage
                    {
                        Value = htmlContent,
                        Representation = "storage"
                    }
                }
            };
            return await _api.CreatePageAsync(_headers, request);
        }

        public async Task DeletePageAsync(string pageId)
        {
            await _api.DeletePageAsync(_headers, pageId);
        }

        public async Task<List<ConfluenceSpace>> ListSpacesAsync(int? maxItems = null)
        {
            ConfluenceSpaces spaces = await _api.ListSpacesAsync(_headers);
            var items = spaces.Results.ToList();
            return maxItems.HasValue ? items.Take(maxItems.Value).ToList() : items;
        }

        public async Task<ConfluenceSpace> GetSpaceAsync(string spaceKey)
        {
            return await _api.GetSpaceAsync(_headers, spaceKey);
        }

        public async Task<List<ConfluencePageSearchResult>> SearchContentByUserAsync(string userAccountId, int? maxItems = null)
        {
            ConfluencePageSearchResults pageResults = await _api.SearchAsync(_headers, userAccountId);
            var items = pageResults.Results;
            return maxItems.HasValue ? items.Take(maxItems.Value).ToList() : items;
        }

        public async Task<List<ConfluencePageSearchResult>> SearchRecentlyModifiedAsync(int days, int maxItems = 100)
        {
            var cql = $"type=page AND lastModified >= \"-{days}d\" ORDER BY lastModified DESC";
            ConfluencePageSearchResults pageResults = await _api.SearchByCqlAsync(_headers, cql, maxItems);
            return pageResults.Results;
        }
    }
}
