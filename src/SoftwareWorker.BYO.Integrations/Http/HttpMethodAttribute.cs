namespace SoftwareWorker.BYO.Integrations.Http
{
    /// <summary>
    /// Declares the HTTP verb and path of an API interface method. The path is relative to the client's
    /// base address and may contain {name} placeholders, in the path or an inline query string, that are
    /// filled from the method parameter with that name (or <see cref="AliasAsAttribute"/> name).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal abstract class HttpMethodAttribute : Attribute
    {
        protected HttpMethodAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public abstract HttpMethod Method { get; }
    }

    internal sealed class GetAttribute : HttpMethodAttribute
    {
        public GetAttribute(string path) : base(path) { }

        public override HttpMethod Method => HttpMethod.Get;
    }

    internal sealed class PostAttribute : HttpMethodAttribute
    {
        public PostAttribute(string path) : base(path) { }

        public override HttpMethod Method => HttpMethod.Post;
    }

    internal sealed class PutAttribute : HttpMethodAttribute
    {
        public PutAttribute(string path) : base(path) { }

        public override HttpMethod Method => HttpMethod.Put;
    }

    internal sealed class PatchAttribute : HttpMethodAttribute
    {
        public PatchAttribute(string path) : base(path) { }

        public override HttpMethod Method => HttpMethod.Patch;
    }

    internal sealed class DeleteAttribute : HttpMethodAttribute
    {
        public DeleteAttribute(string path) : base(path) { }

        public override HttpMethod Method => HttpMethod.Delete;
    }
}
