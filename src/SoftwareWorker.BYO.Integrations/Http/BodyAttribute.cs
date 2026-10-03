namespace SoftwareWorker.BYO.Integrations.Http
{
    /// <summary>
    /// Sends the parameter as the request body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class BodyAttribute : Attribute
    {
        public BodyAttribute(BodySerializationMethod serializationMethod = BodySerializationMethod.Default)
        {
            SerializationMethod = serializationMethod;
        }

        public BodySerializationMethod SerializationMethod { get; }
    }
}
