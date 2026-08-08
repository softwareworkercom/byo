using Refit;
using SoftwareWorker.BYO.Integrations.Confluence.Model;

namespace SoftwareWorker.BYO.Integrations.Confluence
{
    /// <summary>
    /// https://docs.atlassian.com/confluence/REST/latest/
    /// </summary>
    internal interface IConfluenceAPI
    {
        [Get("/wiki/rest/api/content/search?limit=10000&cql=type=page%20and%20(creator='{userAccountId}' OR contributor='{userAccountId}')&expand=history,history.lastUpdated")]
        Task<ConfluencePageSearchResults> SearchAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("userAccountId")] string userAccountId);

        [Get("/wiki/rest/api/content/search?expand=history,history.lastUpdated&cql={cql}&limit={limit}")]
        Task<ConfluencePageSearchResults> SearchByCqlAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("cql")] string cql, [AliasAs("limit")] int limit);

        [Get("/wiki/rest/api/content/{pageId}")]
        Task<ConfluencePage> GetPageAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("pageId")] string pageId);

        [Post("/wiki/rest/api/content")]
        Task<ConfluencePage> CreatePageAsync([HeaderCollection] IDictionary<string, string> headers, [Body] ConfluencePageCreateRequest request);

        [Put("/wiki/rest/api/content/{pageId}")]
        Task<ConfluencePage> UpdatePageAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("pageId")] string pageId, [Body] ConfluencePageUpdateRequest request);

        [Delete("/wiki/rest/api/content/{pageId}")]
        Task DeletePageAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("pageId")] string pageId);

        [Get("/wiki/rest/api/space")]
        Task<ConfluenceSpaces> ListSpacesAsync([HeaderCollection] IDictionary<string, string> headers);

        [Get("/wiki/rest/api/space/{spaceKey}")]
        Task<ConfluenceSpace> GetSpaceAsync([HeaderCollection] IDictionary<string, string> headers, [AliasAs("spaceKey")] string spaceKey);
    }
}
