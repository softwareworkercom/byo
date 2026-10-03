using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.JIRA.Model
{
    public class JiraItem
    {
        [JsonPropertyName("field")]
        public string Field { get; set; }

        [JsonPropertyName("fieldtype")]
        public string Fieldtype { get; set; }

        [JsonPropertyName("fieldId")]
        public string FieldId { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("fromstring")]
        public string FromString { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("tostring")]
        public string ToString { get; set; }

        [JsonPropertyName("tmpfromaccountid")]
        public object TmpFromAccountId { get; set; }

        [JsonPropertyName("tmptoaccountid")]
        public string TmpToAccountId { get; set; }
    }
}