using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SoftwareWorker.BYO.Integrations.Turso;

/// <summary>
/// Connector for performing CRUD operations on Turso database tables.
/// This connector wraps low-level Turso API operations and provides a high-level interface.
/// </summary>
public class TursoConnector : ITursoConnector
{
    private readonly string _apiUrl;
    private readonly string _authToken;
    private static readonly HttpClient _httpClient = new HttpClient();

    /// <summary>
    /// Initializes a new instance of the TursoConnector.
    /// </summary>
    /// <param name="tursoUrl">The Turso database URL (e.g., "database-name.turso.io").</param>
    /// <param name="authToken">The authentication token for Turso.</param>
    public TursoConnector(string tursoUrl, string authToken)
    {
        if (string.IsNullOrWhiteSpace(tursoUrl))
            throw new ArgumentException("Turso URL cannot be null or empty.", nameof(tursoUrl));

        if (string.IsNullOrWhiteSpace(authToken))
            throw new ArgumentException("Auth token cannot be null or empty.", nameof(authToken));

        // Remove protocol prefix if present
        tursoUrl = tursoUrl.Replace("libsql://", "").Replace("https://", "");

        var httpUrl = $"https://{tursoUrl}";
        _apiUrl = $"{httpUrl}/v2/pipeline";
        _authToken = authToken;
    }

    /// <inheritdoc/>
    public async Task<long> CreateAsync<T>(T entity) where T : class
    {
        var tableName = GetTableName<T>();
        var columns = EntityToDictionary(entity);
        return await InsertAndGetIdAsync(tableName, columns);
    }

    /// <inheritdoc/>
    public async Task<T?> ReadByIdAsync<T>(int id) where T : class, new()
    {
        var tableName = GetTableName<T>();
        var sql = $"SELECT * FROM {tableName} WHERE Id = ?";
        var results = await QueryAsync(sql, id);

        if (results.Count == 0)
            return null;

        return DictionaryToEntity<T>(results[0]);
    }

    /// <inheritdoc/>
    public async Task<List<T>> ReadAllAsync<T>() where T : class, new()
    {
        var tableName = GetTableName<T>();
        var sql = $"SELECT * FROM {tableName}";
        var results = await QueryAsync(sql);
        return results.Select(DictionaryToEntity<T>).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<T>> ReadWhereAsync<T>(string whereClause, params object?[] parameters) where T : class, new()
    {
        var tableName = GetTableName<T>();
        var sql = $"SELECT * FROM {tableName} WHERE {whereClause}";
        var results = await QueryAsync(sql, parameters);
        return results.Select(DictionaryToEntity<T>).ToList();
    }

    /// <inheritdoc/>
    public async Task<int> UpdateAsync<T>(int id, T entity) where T : class
    {
        var tableName = GetTableName<T>();
        var columns = EntityToDictionary(entity);

        // Remove Id from columns to update
        columns.Remove("Id");

        return await UpdateInTursoAsync(tableName, columns, "Id = ?", id);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteAsync<T>(int id) where T : class
    {
        var tableName = GetTableName<T>();
        var checkSql = $"SELECT Id FROM {tableName} WHERE Id = ?";
        var sql = $"DELETE FROM {tableName} WHERE Id = ?";

        var existingRows = await ExecuteRawQueryAsync(checkSql, id);
        if (existingRows.Count == 0)
            return 0;

        await ExecuteRawQueryAsync(sql, id);
        return 1;
    }

    private static string GetTableName<T>() => typeof(T).Name;

    /// <inheritdoc/>
    public async Task<List<T>> ExecuteQueryAsync<T>(string sql, params object?[] parameters) where T : class, new()
    {
        var results = await QueryAsync(sql, parameters);
        return results.Select(DictionaryToEntity<T>).ToList();
    }

    /// <inheritdoc/>
    public Task<List<Dictionary<string, object?>>> ExecuteRawQueryAsync(string sql, params object?[] parameters)
    {
        return QueryAsync(sql, parameters);
    }

    #region Private Helper Methods

    private Dictionary<string, object?> EntityToDictionary<T>(T entity) where T : class
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dictionary = new Dictionary<string, object?>();

        foreach (var prop in properties)
        {
            if (IsNotMappedProperty(prop))
                continue;

            if (string.Equals(prop.Name, "Id", StringComparison.Ordinal))
            {
                var idValue = prop.GetValue(entity);
                if (IsDefaultValue(idValue, prop.PropertyType))
                    continue;
            }

            // Skip navigation properties (collections and reference types that are not primitives)
            if (IsNavigationProperty(prop))
                continue;

            var value = prop.GetValue(entity);
            dictionary[prop.Name] = value;
        }

        return dictionary;
    }

    private bool IsNavigationProperty(PropertyInfo property)
    {
        var propertyType = property.PropertyType;

        // Skip collections
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
            return true;

        // Skip complex reference types that are not primitives, strings, or value types
        if (!propertyType.IsPrimitive &&
            !propertyType.IsValueType &&
            propertyType != typeof(string) &&
            propertyType != typeof(DateOnly) &&
            propertyType != typeof(DateTime) &&
            !IsNullable(propertyType))
            return true;

        return false;
    }

    private static bool IsNotMappedProperty(PropertyInfo property)
    {
        return property.GetCustomAttribute<NotMappedAttribute>() is not null;
    }

    private bool IsNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null;
    }

    private static bool IsDefaultValue(object? value, Type targetType)
    {
        if (value == null)
            return true;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (!underlyingType.IsValueType)
            return false;

        var defaultValue = Activator.CreateInstance(underlyingType);
        return Equals(value, defaultValue);
    }

    private T DictionaryToEntity<T>(Dictionary<string, object?> dictionary) where T : class, new()
    {
        var entity = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanWrite)
                continue;

            if (IsNotMappedProperty(prop))
                continue;

            // Skip navigation properties
            if (IsNavigationProperty(prop))
                continue;

            if (dictionary.TryGetValue(prop.Name, out var value))
            {
                if (value != null)
                {
                    // Handle type conversion
                    var convertedValue = ConvertValue(value, prop.PropertyType);
                    prop.SetValue(entity, convertedValue);
                }
            }
        }

        return entity;
    }

    private object? ConvertValue(object value, Type targetType)
    {
        try
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Handle DateOnly
            if (underlyingType == typeof(DateOnly) && value is string dateStr)
            {
                if (DateOnly.TryParse(dateStr, out var dateValue))
                    return dateValue;
                return null;
            }

            // Handle DateTime
            if (underlyingType == typeof(DateTime) && value is string dateTimeStr)
            {
                if (DateTime.TryParse(dateTimeStr, out var dateTimeValue))
                    return dateTimeValue;
                return null;
            }

            // Handle long to int conversion
            if (underlyingType == typeof(int) && value is long longValue)
            {
                return (int) longValue;
            }

            // Handle other type conversions
            if (value.GetType() != underlyingType)
            {
                return Convert.ChangeType(value, underlyingType);
            }

            return value;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error converting value to {targetType.Name}: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Turso API Methods (copied from TursoHelper pattern)

    private async Task<long> InsertAndGetIdAsync(string tableName, Dictionary<string, object?> columns)
    {

        var columnNames = string.Join(", ", columns.Keys);
        var paramPlaceholders = string.Join(", ", columns.Keys.Select(_ => "?"));
        var args = columns.Values.Select(ConvertToTursoValue).ToArray();

        var insertRequest = new
        {
            requests = new object[]
            {
                new
                {
                    type = "execute",
                    stmt = new
                    {
                        sql = $"INSERT INTO {tableName} ({columnNames}) VALUES ({paramPlaceholders})",
                        args
                    }
                },
                new { type = "close" }
            }
        };

        var response = await SendTursoRequestAsync(insertRequest);

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(response);

            if (!doc.RootElement.TryGetProperty("results", out var results))
            {
                throw new InvalidOperationException($"Turso response missing 'results' property. Response: {response}");
            }

            if (results.GetArrayLength() > 0)
            {
                var firstResult = results[0];
                if (firstResult.TryGetProperty("response", out var resultResponse) &&
                    resultResponse.TryGetProperty("result", out var resultObj) &&
                    resultObj.TryGetProperty("last_insert_rowid", out var lastInsertRowId))
                {
                    return long.Parse(lastInsertRowId.GetString() ?? "0");
                }

                // Check for error in response
                if (firstResult.TryGetProperty("error", out var errorObj))
                {
                    var errorMessage = errorObj.TryGetProperty("message", out var msgElement)
                        ? msgElement.GetString()
                        : errorObj.ToString();
                    throw new InvalidOperationException($"Turso insert error: {errorMessage}");
                }
            }

            throw new InvalidOperationException($"Could not get last_insert_rowid from Turso response. Response: {response}");
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse Turso response: {ex.Message}. Response: {response}", ex);
        }
    }

    private async Task<int> UpdateInTursoAsync(string tableName, Dictionary<string, object?> columns, string whereClause, params object?[] whereParams)
    {

        var setClause = string.Join(", ", columns.Keys.Select(k => $"{k} = ?"));
        var args = columns.Values.Select(ConvertToTursoValue)
                          .Concat(whereParams.Select(ConvertToTursoValue))
                          .ToArray();

        var updateRequest = new
        {
            requests = new object[]
            {
                new
                {
                    type = "execute",
                    stmt = new
                    {
                        sql = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}",
                        args
                    }
                },
                new { type = "close" }
            }
        };

        var response = await SendTursoRequestAsync(updateRequest);

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(response);

            if (!doc.RootElement.TryGetProperty("results", out var results))
            {
                throw new InvalidOperationException($"Turso response missing 'results' property. Response: {response}");
            }

            if (results.GetArrayLength() > 0)
            {
                var firstResult = results[0];

                // Check for error in response
                if (firstResult.TryGetProperty("error", out var errorObj))
                {
                    var errorMessage = errorObj.TryGetProperty("message", out var msgElement)
                        ? msgElement.GetString()
                        : errorObj.ToString();
                    throw new InvalidOperationException($"Turso update error: {errorMessage}");
                }

                if (firstResult.TryGetProperty("response", out var resultResponse) &&
                    resultResponse.TryGetProperty("result", out var resultObj) &&
                    resultObj.TryGetProperty("affected_row_count", out var affectedRows))
                {
                    return affectedRows.GetInt32();
                }
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse Turso response: {ex.Message}. Response: {response}", ex);
        }

        return 0;
    }

    private async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, params object?[] parameters)
    {

        var args = parameters.Select(ConvertToTursoValue).ToArray();

        var queryRequest = new
        {
            requests = new object[]
            {
                new
                {
                    type = "execute",
                    stmt = new
                    {
                        sql,
                        args
                    }
                },
                new { type = "close" }
            }
        };

        var response = await SendTursoRequestAsync(queryRequest);
        return ParseQueryResults(response);
    }

    private async Task<string> SendTursoRequestAsync(object request)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
        {
            Content = content
        };
        requestMessage.Headers.Add("Authorization", $"Bearer {_authToken}");

        var response = await _httpClient.SendAsync(requestMessage);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Turso API error ({response.StatusCode}): {responseContent}");
        }

        return responseContent;
    }

    private List<Dictionary<string, object?>> ParseQueryResults(string jsonResponse)
    {
        var rows = new List<Dictionary<string, object?>>();

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(jsonResponse);

            if (!doc.RootElement.TryGetProperty("results", out var results))
                return rows;

            if (results.GetArrayLength() == 0) return rows;

            var firstResult = results[0];
            if (!firstResult.TryGetProperty("response", out var response))
                return rows;

            if (!response.TryGetProperty("result", out var result)) return rows;

            if (!result.TryGetProperty("cols", out var cols) ||
                !result.TryGetProperty("rows", out var rowsArray))
                return rows;

            var columnNames = new List<string>();
            foreach (var col in cols.EnumerateArray())
            {
                if (col.TryGetProperty("name", out var nameElement))
                {
                    columnNames.Add(nameElement.GetString() ?? "");
                }
                else
                {
                    columnNames.Add("");
                }
            }

            foreach (var row in rowsArray.EnumerateArray())
            {
                var rowDict = new Dictionary<string, object?>();
                for (int i = 0; i < columnNames.Count; i++)
                {
                    var cell = row[i];
                    if (cell.TryGetProperty("type", out var typeElement))
                    {
                        var type = typeElement.GetString();
                        try
                        {
                            rowDict[columnNames[i]] = type switch
                            {
                                "null" => null,
                                "integer" => cell.TryGetProperty("value", out var intVal)
                                    ? long.Parse(intVal.GetString() ?? "0")
                                    : 0L,
                                "float" => cell.TryGetProperty("value", out var floatVal)
                                    ? floatVal.GetDouble()
                                    : 0.0,
                                "text" => cell.TryGetProperty("value", out var textVal)
                                    ? textVal.GetString()
                                    : null,
                                "blob" => cell.TryGetProperty("base64", out var blobVal)
                                    ? Convert.FromBase64String(blobVal.GetString() ?? "")
                                    : null,
                                _ => cell.TryGetProperty("value", out var defaultVal)
                                    ? defaultVal.ToString()
                                    : null
                            };
                        }
                        catch (Exception ex)
                        {
                            // Log or handle type conversion errors - set to null for now
                            System.Diagnostics.Debug.WriteLine($"Error parsing column {columnNames[i]}: {ex.Message}");
                            rowDict[columnNames[i]] = null;
                        }
                    }
                    else
                    {
                        rowDict[columnNames[i]] = null;
                    }
                }
                rows.Add(rowDict);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse Turso query results: {ex.Message}. Response: {jsonResponse}", ex);
        }

        return rows;
    }

    private object ConvertToTursoValue(object? value)
    {
        return value switch
        {
            null => new { type = "null" },
            int i => new { type = "integer", value = i.ToString() },
            long l => new { type = "integer", value = l.ToString() },
            double d => new { type = "float", value = d },
            float f => new { type = "float", value = (double) f },
            bool b => new { type = "integer", value = b ? "1" : "0" },
            DateOnly date => new { type = "text", value = date.ToString("yyyy-MM-dd") },
            DateTime dt => new { type = "text", value = dt.ToString("yyyy-MM-dd HH:mm:ss") },
            string s => new { type = "text", value = s },
            _ => new { type = "text", value = value.ToString() ?? "" }
        };
    }

    #endregion
}


