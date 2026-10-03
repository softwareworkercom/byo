namespace SoftwareWorker.BYO.Integrations.Http
{
    /// <summary>
    /// Overrides the name a parameter is bound to in the path placeholders and the query string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class AliasAsAttribute : Attribute
    {
        public AliasAsAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
