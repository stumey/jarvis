using Json.Schema;
using System.Reflection;
using System.Text.Json;

namespace Shared.Validation;

/// <summary>
/// Loads event schema from various sources.
/// </summary>
public static class EventSchemaLoader
{
    /// <summary>
    /// Loads schema from embedded resource in the Shared assembly.
    /// </summary>
    public static JsonSchema LoadFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Shared.Schemas.event_schema_v1.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        return LoadFromStream(stream);
    }

    /// <summary>
    /// Loads schema from a file path.
    /// </summary>
    public static JsonSchema LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Schema file not found: {filePath}");

        using var stream = File.OpenRead(filePath);
        return LoadFromStream(stream);
    }

    /// <summary>
    /// Loads schema from a stream.
    /// </summary>
    public static JsonSchema LoadFromStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var schemaJson = reader.ReadToEnd();
        return LoadFromString(schemaJson);
    }

    /// <summary>
    /// Loads schema from a JSON string.
    /// </summary>
    public static JsonSchema LoadFromString(string schemaJson)
    {
        return JsonSerializer.Deserialize<JsonSchema>(schemaJson)
            ?? throw new InvalidOperationException("Failed to deserialize JSON schema");
    }
}
