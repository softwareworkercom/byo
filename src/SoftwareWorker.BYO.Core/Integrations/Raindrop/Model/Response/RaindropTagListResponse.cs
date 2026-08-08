using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Raindrop.Model.Response
{
    public class RaindropTagListResponse
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }

        [JsonPropertyName("items")]
        public List<RaindropTag>? Items { get; set; }
    }
}
