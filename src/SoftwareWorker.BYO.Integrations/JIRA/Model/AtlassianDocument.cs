using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class AtlassianDocument
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "doc";

        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("content")]
        public List<AtlassianDocumentNode> Content { get; set; } = [];

        public static AtlassianDocument FromPlainText(string text)
        {
            return new AtlassianDocument
            {
                Content = text.Split('\n').Select(line => new AtlassianDocumentNode
                {
                    Type = "paragraph",
                    Content =
                    [
                        new AtlassianDocumentNode
                        {
                            Type = "text",
                            Text = line
                        }
                    ]
                }).ToList()
            };
        }
    }

    public class AtlassianDocumentNode
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<AtlassianDocumentNode>? Content { get; set; }
    }
}
