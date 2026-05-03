# Turso Integration Connector

The Turso Integration Connector provides a high-level interface for performing CRUD (Create, Read, Update, Delete) operations on Turso database tables. Turso is a distributed SQLite database built on libSQL.

## Overview

The connector abstracts the low-level Turso HTTP API calls and provides a simple, type-safe interface for working with entities in your Turso database.

## Features

- **Create**: Insert new records and get the generated ID
- **Read**: Query records by ID, retrieve all records, or filter with WHERE clauses
- **Update**: Modify existing records
- **Delete**: Remove records by ID
- **Custom Queries**: Execute custom SQL queries and map results to entities

## Installation

The connector is part of the `SoftwareWorker.BYO.Integrations` project. No additional packages are required.

## Usage

### Creating a Connector Instance

```csharp
using SoftwareWorker.BYO.Integrations.Turso;

// Initialize the connector with your Turso credentials
var connector = new TursoConnector(
    tursoUrl: "your-database.turso.io",      // or "libsql://your-database.turso.io"
    authToken: "your-auth-token"
);
```

### Create Operation

```csharp
var employee = new Employee
{
    FirstName = "John",
    LastName = "Doe",
    RoleId = 1,
    LocationId = 1,
    WorkEmail = "john.doe@example.com"
};

// Insert and get the new ID
long newId = await connector.CreateAsync("Employee", employee);
```

### Read Operations

```csharp
// Read by ID
var employee = await connector.ReadByIdAsync<Employee>("Employee", 123);

// Read all records
var allEmployees = await connector.ReadAllAsync<Employee>("Employee");

// Read with WHERE clause
var filteredEmployees = await connector.ReadWhereAsync<Employee>(
    "Employee",
    "RoleId = ? AND LocationId = ?",
    1, 2
);
```

### Update Operation

```csharp
// Update an existing record
employee.FirstName = "Jane";
employee.WorkEmail = "jane.doe@example.com";

int affectedRows = await connector.UpdateAsync("Employee", employee.Id, employee);
```

### Delete Operation

```csharp
// Delete by ID
int affectedRows = await connector.DeleteAsync("Employee", 123);
```

### Custom Queries

```csharp
// Execute a custom SQL query
var results = await connector.ExecuteQueryAsync<Employee>(
    "SELECT * FROM Employee WHERE RoleId = ? ORDER BY LastName",
    1
);
```

## Entity Mapping

The connector automatically maps between C# entities and Turso database records using reflection. 

### Supported Types

- Primitive types: `int`, `long`, `double`, `float`, `bool`
- Strings: `string`
- Dates: `DateOnly`, `DateTime`
- Nullable versions of all the above

### Navigation Properties

The connector automatically skips navigation properties (collections and complex reference types) during mapping. Only scalar properties are included in CRUD operations.

## Configuration

### Getting Turso Credentials

1. **Install Turso CLI**:
   ```bash
   # Windows
   winget install turso
   
   # macOS
   brew install tursodatabase/tap/turso
   
   # Linux
   curl -sSfL https://get.tur.so/install.sh | bash
   ```

2. **Login to Turso**:
   ```bash
   turso auth login
   ```

3. **Create or list databases**:
   ```bash
   # List existing databases
   turso db list
   
   # Create a new database
   turso db create my-database
   ```

4. **Get database URL**:
   ```bash
   turso db show my-database
   ```

5. **Generate auth token**:
   ```bash
   turso db tokens create my-database
   ```

### User Secrets Configuration

For testing, configure your Turso credentials in user secrets:

```json
{
  "Turso": {
    "Url": "your-database.turso.io",
    "AuthToken": "your-auth-token-here"
  }
}
```

## API Reference

### ITursoConnector Interface

```csharp
public interface ITursoConnector
{
    Task<long> CreateAsync<T>(string tableName, T entity) where T : class;
    Task<T?> ReadByIdAsync<T>(string tableName, int id) where T : class, new();
    Task<List<T>> ReadAllAsync<T>(string tableName) where T : class, new();
    Task<List<T>> ReadWhereAsync<T>(string tableName, string whereClause, params object?[] parameters) where T : class, new();
    Task<int> UpdateAsync<T>(string tableName, int id, T entity) where T : class;
    Task<int> DeleteAsync(string tableName, int id);
    Task<List<T>> ExecuteQueryAsync<T>(string sql, params object?[] parameters) where T : class, new();
}
```

## Error Handling

The connector throws exceptions for various error conditions:

- `ArgumentException`: Invalid constructor parameters (empty URL or token)
- `InvalidOperationException`: Turso API errors, parsing errors
- `HttpRequestException`: Network or HTTP-level errors

Example error handling:

```csharp
try
{
    var employee = await connector.ReadByIdAsync<Employee>("Employee", 123);
}
catch (InvalidOperationException ex)
{
    // Handle Turso API errors
    Console.WriteLine($"Turso error: {ex.Message}");
}
catch (HttpRequestException ex)
{
    // Handle network errors
    Console.WriteLine($"Network error: {ex.Message}");
}
```

## Performance Considerations

- **Network Latency**: All operations involve HTTP calls to Turso, so expect network latency
- **Batch Operations**: For bulk inserts/updates, consider using transactions via custom queries
- **Connection Reuse**: The connector uses a static HttpClient instance for efficient connection reuse

## Testing

The connector includes comprehensive integration tests in `TursoIntegrationTests.cs`. To run the tests:

1. Configure Turso credentials in user secrets (see Configuration section)
2. Run the tests:
   ```bash
   dotnet test --filter "FullyQualifiedName~TursoIntegrationTests"
   ```

## Security Best Practices

1. **Never commit auth tokens** to version control
2. Use **environment variables** or **secret managers** for credentials
3. **Rotate tokens** regularly
4. Use **short-lived tokens** for CI/CD pipelines

## Related Documentation

- [Turso Documentation](https://docs.turso.tech/)
- [Turso CLI Reference](https://docs.turso.tech/reference/turso-cli)
- [libSQL Project](https://github.com/tursodatabase/libsql)

## License

This connector is part of the SoftwareWorker platform and follows the same license.
