namespace SoftwareWorker.BYO.Integrations.Http
{
    /// <summary>
    /// Sends the parameter as a query string value. Null values are omitted.
    /// Parameters that don't match a path placeholder are sent in the query string even without this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class QueryAttribute : Attribute
    {
    }
}
