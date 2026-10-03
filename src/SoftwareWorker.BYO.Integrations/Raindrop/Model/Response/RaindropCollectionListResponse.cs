using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Raindrop.Model.Response
{
    public class RaindropCollectionListResponse
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }

        [JsonPropertyName("items")]
        public List<RaindropCollection>? Items { get; set; }
    }
}
