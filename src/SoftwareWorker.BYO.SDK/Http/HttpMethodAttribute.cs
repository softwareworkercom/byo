namespace SoftwareWorker.BYO.SDK.Http
{
    /// <summary>
    /// Declares the HTTP verb and path of an API interface method. The path is relative to the client's
    /// base address and may contain {name} placeholders, in the path or an inline query string, that are
    /// filled from the method parameter with that name (or <see cref="AliasAsAttribute"/> name).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class HttpMethodAttribute : Attribute
    {
        protected HttpMethodAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public abstract HttpMethod Method { get; }
    }

    public sealed class GetAttribute : HttpMethodAttribute
    {
        public GetAttribute(string path) : base(path) { }

        public override HttpMethod Method => HttpMethod.Get;
    }

    public sealed class PostAttribute : HttpMethodAttribute
    {
        public PostAttribute(string path) : base(path) { }

        public override HttpMethod Method => HttpMethod.Post;
    }

    public sealed class PutAttribute : HttpMethodAttribute
    {
        public PutAttribute(string path) : base(path) { }

        public override HttpMethod Method => HttpMethod.Put;
    }

    public sealed class PatchAttribute : HttpMethodAttribute
    {
        public PatchAttribute(string path) : base(path) { }

        public override HttpMethod Method => HttpMethod.Patch;
    }

    public sealed class DeleteAttribute : HttpMethodAttribute
    {
        public DeleteAttribute(string path) : base(path) { }

        public override HttpMethod Method => HttpMethod.Delete;
    }
}
