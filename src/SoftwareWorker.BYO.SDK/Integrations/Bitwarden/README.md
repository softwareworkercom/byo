# Bitwarden Integration Connector

A comprehensive connector for integrating with Bitwarden Secrets Manager API in .NET applications.

## Overview

This connector provides a .NET client for the Bitwarden Secrets Manager API, enabling programmatic access to create, retrieve, update, and delete secrets within Bitwarden organizations and projects.

## Features

- **Secrets Management**: Create, read, update, and delete secrets
- **Project Support**: List and retrieve projects
- **Organization Support**: Filter secrets by organization
- **Resilience**: Built-in retry logic with exponential backoff
- **Verbose Logging**: Optional detailed logging for debugging

## Prerequisites

- Bitwarden account with Secrets Manager enabled
- Organization with access to Secrets Manager
- Access token for authentication

## Configuration

### Getting Bitwarden Access Token

1. Sign in to your [Bitwarden Web Vault](https://vault.bitwarden.com/)
2. Navigate to your **Organization**
3. Go to **Settings** → **Secrets Manager**
4. Create a new **Machine Account** or use an existing one
5. Generate an **Access Token** for the machine account
6. Copy the access token immediately (it won't be shown again)

**Note**: Access tokens provide full access to secrets within the authorized scope. Store them securely and never commit them to version control.

### User Secrets Configuration

For testing and development, configure your Bitwarden credentials in user secrets:

```json
{
  "Bitwarden": {
    "ApiUrl": "https://api.bitwarden.com",
    "AccessToken": "your-access-token-here",
    "OrganizationId": "your-organization-id"
  }
}
```

To set user secrets:

```bash
dotnet user-secrets set "Bitwarden:ApiUrl" "https://api.bitwarden.com"
dotnet user-secrets set "Bitwarden:AccessToken" "your-access-token-here"
dotnet user-secrets set "Bitwarden:OrganizationId" "your-organization-id"
```

### Environment Variables

For production deployments, use environment variables:

```bash
export BITWARDEN__APIURL="https://api.bitwarden.com"
export BITWARDEN__ACCESSTOKEN="your-access-token-here"
export BITWARDEN__ORGANIZATIONID="your-organization-id"
```

## Usage

### Basic Setup

```csharp
using SoftwareWorker.BYO.Integrations.Bitwarden;

// Initialize connector
var apiUrl = "https://api.bitwarden.com";
var accessToken = "your-access-token";
var connector = new BitwardenConnector(apiUrl, accessToken, isVerbose: true);
```

### Creating a Secret

```csharp
var response = await connector.CreateSecretAsync(
    organizationId: "your-org-id",
    key: "DATABASE_PASSWORD",
    value: "super-secret-password",
    note: "Production database password",
    projectIds: new List<string> { "project-id-1" }
);

Console.WriteLine($"Secret created with ID: {response?.Id}");
```

### Listing Secrets

```csharp
var secrets = await connector.ListSecretsAsync(organizationId: "your-org-id");
foreach (var secret in secrets ?? new())
{
    Console.WriteLine($"Key: {secret.Key}, ID: {secret.Id}");
}
```

### Getting a Secret

```csharp
var secret = await connector.GetSecretAsync("secret-id");
Console.WriteLine($"Key: {secret?.Key}");
Console.WriteLine($"Value: {secret?.Value}");
```

### Updating a Secret

```csharp
var updated = await connector.UpdateSecretAsync(
    secretId: "secret-id",
    key: "DATABASE_PASSWORD",
    value: "new-super-secret-password"
);
```

### Deleting a Secret

```csharp
var deleted = await connector.DeleteSecretAsync("secret-id");
Console.WriteLine($"Secret deleted: {deleted}");
```

### Listing Projects

```csharp
var projects = await connector.ListProjectsAsync(organizationId: "your-org-id");
foreach (var project in projects ?? new())
{
    Console.WriteLine($"Project: {project.Name}, ID: {project.Id}");
}
```

## API Reference

### BitwardenConnector

#### Constructor

```csharp
public BitwardenConnector(string apiUrl, string accessToken, bool isVerbose = false)
```

- `apiUrl`: Base URL for the Bitwarden API (typically `https://api.bitwarden.com`)
- `accessToken`: Machine account access token
- `isVerbose`: Enable verbose logging (default: false)

#### Methods

##### CreateSecretAsync

```csharp
public async Task<BitwardenSecretResponse?> CreateSecretAsync(
    string organizationId,
    string key,
    string value,
    string? note = null,
    List<string>? projectIds = null)
```

Creates a new secret in the specified organization.

##### ListSecretsAsync

```csharp
public async Task<List<BitwardenSecret>?> ListSecretsAsync(string? organizationId = null)
```

Lists all secrets accessible by the access token, optionally filtered by organization.

##### GetSecretAsync

```csharp
public async Task<BitwardenSecretResponse?> GetSecretAsync(string secretId)
```

Retrieves a specific secret by its ID.

##### UpdateSecretAsync

```csharp
public async Task<BitwardenSecretResponse?> UpdateSecretAsync(
    string secretId,
    string? key = null,
    string? value = null,
    string? note = null,
    List<string>? projectIds = null)
```

Updates an existing secret. Only provided fields will be updated.

##### DeleteSecretAsync

```csharp
public async Task<bool> DeleteSecretAsync(string secretId)
```

Deletes a secret by its ID. Returns `true` if successful.

##### ListProjectsAsync

```csharp
public async Task<List<BitwardenProject>?> ListProjectsAsync(string? organizationId = null)
```

Lists all projects accessible by the access token, optionally filtered by organization.

##### GetProjectAsync

```csharp
public async Task<BitwardenProject?> GetProjectAsync(string projectId)
```

Retrieves a specific project by its ID.

## Error Handling

The connector uses resilience patterns with automatic retry logic. All API calls are wrapped in `ResilienceHelper.ExecuteWithResilienceAsync()`, which provides:

- Automatic retries with exponential backoff
- Circuit breaker pattern
- Proper exception handling

## Testing

Integration tests are available in `SoftwareWorker.BYO.Integrations.Test`. To run tests:

1. Configure Bitwarden credentials in user secrets (see Configuration section)
2. Run the tests:
   ```bash
   dotnet test --filter "FullyQualifiedName~BitwardenIntegrationTests"
   ```

## Security Best Practices

1. **Never commit access tokens** to version control
2. Use **environment variables** or **secret managers** for credentials in production
3. **Rotate tokens** regularly
4. Use **machine accounts** with minimal required permissions
5. Enable **audit logging** in Bitwarden to track secret access
6. Use **projects** to organize and scope secrets appropriately

## Self-Hosted Bitwarden

If you're using a self-hosted Bitwarden instance, change the API URL:

```csharp
var connector = new BitwardenConnector(
    apiUrl: "https://your-bitwarden-instance.com",
    accessToken: "your-access-token",
    isVerbose: true
);
```

## Related Documentation

- [Bitwarden Secrets Manager](https://bitwarden.com/products/secrets-manager/)
- [Bitwarden Secrets Manager SDK](https://bitwarden.com/help/secrets-manager-sdk/)
- [Bitwarden API Documentation](https://bitwarden.com/help/bitwarden-apis/)

## License

This connector is part of the SoftwareWorker platform and follows the same license.
