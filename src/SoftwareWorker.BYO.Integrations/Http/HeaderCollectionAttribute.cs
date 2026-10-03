namespace SoftwareWorker.BYO.Integrations.Http
{
    /// <summary>
    /// Adds every entry of an <see cref="IDictionary{TKey, TValue}"/> parameter as a request header.
    /// Content headers such as Content-Type replace the body's own. A request other than GET/HEAD without a
    /// body is given an empty one to carry them; on GET/HEAD they are dropped.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class HeaderCollectionAttribute : Attribute
    {
    }
}
