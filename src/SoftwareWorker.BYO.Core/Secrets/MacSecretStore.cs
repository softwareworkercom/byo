using System.Diagnostics;

namespace SoftwareWorker.BYO.Core.Secrets
{
    public class MacSecretStore : ISecretStore
    {
        public void SetSecret(string key, string secret)
        {
            Run($"add-generic-password -a \"app\" -s \"{key}\" -w \"{secret}\" -U");
        }

        public string GetSecret(string key)
        {
            return RunOutput($"find-generic-password -a \"app\" -s \"{key}\" -w");
        }

        public void DeleteSecret(string key)
        {
            Run($"delete-generic-password -a \"app\" -s \"{key}\"");
        }

        private void Run(string args)
        {
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "security",
                Arguments = args,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new Exception("Keychain command failed");
        }

        private string RunOutput(string args)
        {
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "security",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            string output = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();

            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new Exception(error);

            return output.Trim();
        }
    }
}