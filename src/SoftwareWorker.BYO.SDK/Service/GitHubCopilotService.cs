using GitHub.Copilot;
using System.Text;

namespace SoftwareWorker.BYO.CLI.Core.Service
{
    public sealed class GitHubCopilotService
    {
        private readonly CopilotClientOptions _clientOptions;

        public GitHubCopilotService(CopilotClientOptions? clientOptions = null)
        {
            _clientOptions = clientOptions ?? new CopilotClientOptions();
        }

        public async Task<string> SendPromptAsync(
            string prompt,
            SessionConfig? sessionConfig = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

            await using var client = new CopilotClient(_clientOptions);
            await client.StartAsync(cancellationToken);

            await using var session = await client.CreateSessionAsync(
                sessionConfig ?? new SessionConfig(),
                cancellationToken);

            var response = new StringBuilder();
            using var subscription = session.On<AssistantMessageEvent>(message =>
            {
                if (!string.IsNullOrEmpty(message.Data.Content))
                {
                    response.Append(message.Data.Content);
                }
            });

            await session.SendAndWaitAsync(
                new MessageOptions { Prompt = prompt },
                timeout,
                cancellationToken);

            return response.ToString();
        }
    }
}
