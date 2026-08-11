namespace SoftwareWorker.BYO.Integrations.Turso;

/// <summary>
/// Interface for Turso database connector supporting CRUD operations.
/// Table names are inferred from the entity class name.
/// </summary>
public interface ITursoConnector
{
    /// <summary>
    /// Creates a new record in the table (inferred from class name).
    /// </summary>
    /// <typeparam name="T">The entity type. Table name is inferred from class name.</typeparam>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>The ID of the newly created record.</returns>
    Task<long> CreateAsync<T>(T entity) where T : class;

    /// <summary>
    /// Reads a single record by ID.
    /// </summary>
    /// <typeparam name="T">The entity type. Table name is inferred from class name.</typeparam>
    /// <param name="id">The ID of the record.</param>
    /// <returns>The entity or null if not found.</returns>
    Task<T?> ReadByIdAsync<T>(int id) where T : class, new();

    /// <summary>
    /// Reads all records from a table.
    /// </summary>
    /// <typeparam name="T">The entity type. Table name is inferred from class name.</typeparam>
    /// <returns>A list of entities.</returns>
    Task<List<T>> ReadAllAsync<T>() where T : class, new();

    /// <summary>
    /// Reads records matching a WHERE clause.
    /// </summary>
    /// <typeparam name="T">The entity type. Table name is inferred from class name.</typeparam>
    /// <param name="whereClause">The WHERE clause (without WHERE keyword).</param>
    /// <param name="parameters">Parameters for the WHERE clause.</param>
    /// <returns>A list of matching entities.</returns>
    Task<List<T>> ReadWhereAsync<T>(string whereClause, params object?[] parameters) where T : class, new();

    /// <summary>
    /// Updates a record in the table (inferred from class name).
    /// </summary>
    /// <typeparam name="T">The entity type. Table name is inferred from class name.</typeparam>
    /// <param name="id">The ID of the record to update.</param>
    /// <param name="entity">The entity with updated values.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> UpdateAsync<T>(int id, T entity) where T : class;

    /// <summary>
    /// Deletes a record by ID.
    /// </summary>
    /// <typeparam name="T">The entity type. Table name is inferred from class name.</typeparam>
    /// <param name="id">The ID of the record to delete.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> DeleteAsync<T>(int id) where T : class;

    /// <summary>
    /// Executes a SQL query and returns raw rows as dictionaries.
    /// </summary>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="parameters">Parameters for the query.</param>
    /// <returns>Query rows as key/value dictionaries.</returns>
    Task<List<Dictionary<string, object?>>> ExecuteRawQueryAsync(string sql, params object?[] parameters);
}
