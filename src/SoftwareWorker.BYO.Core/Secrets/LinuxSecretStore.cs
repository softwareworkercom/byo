using System;
using System.Diagnostics;

namespace SoftwareWorker.BYO.Core.Secrets
{
    public class LinuxSecretStore : ISecretStore
    {
        public void SetSecret(string key, string secret)
        {
            RunWithInput($"store --label=\"app\" service app key {key}", secret);
        }

        public string GetSecret(string key)
        {
            return RunOutput($"lookup service app key {key}");
        }

        public void DeleteSecret(string key)
        {
            Run($"clear service app key {key}");
        }

        private void Run(string args)
        {
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "secret-tool",
                Arguments = args,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new Exception("Keyring command failed");
        }

        private void RunWithInput(string args, string input)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "secret-tool",
                    Arguments = args,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            p.Start();
            p.StandardInput.Write(input);
            p.StandardInput.Close();

            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new Exception("Keyring store failed");
        }

        private string RunOutput(string args)
        {
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "secret-tool",
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