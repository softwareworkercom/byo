using System.Runtime.InteropServices;

namespace SoftwareWorker.BYO.SDK.Vault
{
    internal sealed class VaultProvider
    {
        private static readonly Lazy<IVault> _instance =
            new Lazy<IVault>(CreateVault, isThreadSafe: true);

        private VaultProvider() { }

        public static IVault Instance => _instance.Value;

        private static IVault CreateVault()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsVault();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new MacVault();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return new LinuxVault();

            throw new PlatformNotSupportedException("Unsupported OS");
        }
    }
}