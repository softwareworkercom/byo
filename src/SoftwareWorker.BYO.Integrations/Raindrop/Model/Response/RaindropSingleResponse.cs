using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.Integrations.Raindrop.Model.Response
{
    public class RaindropSingleResponse
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }

        [JsonPropertyName("item")]
        public RaindropItem? Item { get; set; }
    }
}
