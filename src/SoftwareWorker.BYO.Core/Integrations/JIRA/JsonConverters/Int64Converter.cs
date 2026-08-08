using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoftwareWorker.BYO.IntegrationsCommon.JsonConverters
{
    public class Int64Converter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string stringValue = reader.GetString();
                if (long.TryParse(stringValue, out long value))
                {
                    return value;
                }
                else
                {
                    return 0;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt64();
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}