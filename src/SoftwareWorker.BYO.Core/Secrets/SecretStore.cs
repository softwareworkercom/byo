using System.Runtime.InteropServices;

namespace SoftwareWorker.BYO.Core.Secrets
{
    public sealed class SecretStore
    {
        private static readonly Lazy<ISecretStore> _instance =
            new Lazy<ISecretStore>(CreateStore, isThreadSafe: true);

        private SecretStore() { }

        public static ISecretStore Instance => _instance.Value;

        private static ISecretStore CreateStore()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsSecretStore();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new MacSecretStore();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return new LinuxSecretStore();

            throw new PlatformNotSupportedException("Unsupported OS");
        }
    }
}