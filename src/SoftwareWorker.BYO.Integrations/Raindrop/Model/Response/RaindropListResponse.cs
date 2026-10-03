using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Raindrop.Model.Response
{
    public class RaindropListResponse
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }

        [JsonPropertyName("items")]
        public List<RaindropItem>? Items { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
