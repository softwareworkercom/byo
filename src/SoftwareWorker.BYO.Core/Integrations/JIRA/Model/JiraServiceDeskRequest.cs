using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraServiceDeskRequest
    {
        [JsonPropertyName("issueId")]
        public string IssueId { get; set; } = string.Empty;

        [JsonPropertyName("issueKey")]
        public string IssueKey { get; set; } = string.Empty;

        [JsonPropertyName("requestFieldValues")]
        public List<JiraServiceDeskFieldValue> RequestFieldValues { get; set; } = [];

        [JsonPropertyName("currentStatus")]
        public JiraServiceDeskCurrentStatus? CurrentStatus { get; set; }

        [JsonPropertyName("requestType")]
        public JiraServiceDeskRequestType? RequestType { get; set; }

        [JsonPropertyName("serviceDesk")]
        public JiraServiceDeskInfo? ServiceDesk { get; set; }

        [JsonPropertyName("reporter")]
        public JiraUser? Reporter { get; set; }

        [JsonPropertyName("comment")]
        public JiraServiceDeskCommentContainer? Comment { get; set; }

        public string? GetFieldValue(string fieldId) =>
            RequestFieldValues
                .FirstOrDefault(f => f.FieldId.Equals(fieldId, StringComparison.OrdinalIgnoreCase))
                ?.ValueAsString;
    }

    public class JiraServiceDeskFieldValue
    {
        [JsonPropertyName("fieldId")]
        public string FieldId { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        public string? ValueAsString => Value?.ToString();
    }

    public class JiraServiceDeskCurrentStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class JiraServiceDeskRequestType
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class JiraServiceDeskInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("projectKey")]
        public string ProjectKey { get; set; } = string.Empty;

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;
    }

    public class JiraServiceDeskCommentContainer
    {
        [JsonPropertyName("values")]
        public List<JiraServiceDeskComment> Values { get; set; } = [];
    }

    public class JiraServiceDeskComment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public JiraUser? Author { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public JiraServiceDeskDate? Created { get; set; }
    }

    public class JiraServiceDeskDate
    {
        [JsonPropertyName("iso8601")]
        public string? Iso8601 { get; set; }

        [JsonPropertyName("friendly")]
        public string? Friendly { get; set; }
    }
}
