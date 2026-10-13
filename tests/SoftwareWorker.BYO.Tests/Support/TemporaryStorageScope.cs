using SoftwareWorker.BYO.SDK.Services;
using SoftwareWorker.BYO.SDK.Vault;

namespace SoftwareWorker.BYO.Tests.Support;

/// <summary>
/// Points every storage-backed service (settings, secrets, commands, workflows) at files inside a temporary
/// folder, optionally with its own encryption key, and restores the original locations when disposed.
/// </summary>
internal sealed class TemporaryStorageScope : IDisposable
{
    private readonly string _originalSettingsFilePath = SettingsService.SettingsFilePath;
    private readonly string _originalSettingsSecretsFilePath = SettingsService.SecretsFilePath;
    private readonly string _originalSecretsFilePath = SecretsService.SecretsFilePath;
    private readonly string _originalSecretsSettingsFilePath = SecretsService.SettingsFilePath;
    private readonly string _originalCommandsFilePath = CommandService.CommandsFilePath;
    private readonly string _originalWorkflowsFilePath = WorkflowService.WorkflowsFilePath;
    private readonly string _originalSecretKeyName = KeyManagementService.SecretKeyName;
    private readonly bool _ownsEncryptionKey;

    public TemporaryStorageScope(bool isolateEncryptionKey = false)
    {
        Root = Path.Combine(Path.GetTempPath(), "byo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);

        SettingsFilePath = Path.Combine(Root, "settings.json");
        SecretsFilePath = Path.Combine(Root, "secrets.json");
        CommandsFilePath = Path.Combine(Root, "commands.json");
        WorkflowsFilePath = Path.Combine(Root, "workflows.json");

        SettingsService.SettingsFilePath = SettingsFilePath;
        SettingsService.SecretsFilePath = SecretsFilePath;
        SecretsService.SecretsFilePath = SecretsFilePath;
        SecretsService.SettingsFilePath = SettingsFilePath;
        CommandService.CommandsFilePath = CommandsFilePath;
        WorkflowService.WorkflowsFilePath = WorkflowsFilePath;

        if (isolateEncryptionKey)
        {
            KeyManagementService.SecretKeyName = $"byo-key-tests-{Guid.NewGuid():N}";
            _ownsEncryptionKey = true;
        }
    }

    public string Root { get; }

    public string SettingsFilePath { get; }

    public string SecretsFilePath { get; }

    public string CommandsFilePath { get; }

    public string WorkflowsFilePath { get; }

    public void Dispose()
    {
        if (_ownsEncryptionKey)
        {
            try
            {
                VaultProvider.Instance.DeleteSecret(KeyManagementService.SecretKeyName);
            }
            catch
            {
                // The key may never have been created, or the platform vault may be unavailable.
            }
        }

        SettingsService.SettingsFilePath = _originalSettingsFilePath;
        SettingsService.SecretsFilePath = _originalSettingsSecretsFilePath;
        SecretsService.SecretsFilePath = _originalSecretsFilePath;
        SecretsService.SettingsFilePath = _originalSecretsSettingsFilePath;
        CommandService.CommandsFilePath = _originalCommandsFilePath;
        WorkflowService.WorkflowsFilePath = _originalWorkflowsFilePath;
        KeyManagementService.SecretKeyName = _originalSecretKeyName;

        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of temporary files.
        }
    }
}
