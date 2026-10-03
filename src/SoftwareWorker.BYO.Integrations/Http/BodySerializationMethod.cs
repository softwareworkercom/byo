namespace SoftwareWorker.BYO.Integrations.Http
{
    internal enum BodySerializationMethod
    {
        /// <summary>Objects are serialized as JSON; strings and <see cref="HttpContent"/> are sent as-is.</summary>
        Default,

        /// <summary>A dictionary is sent as application/x-www-form-urlencoded fields.</summary>
        UrlEncoded
    }
}
