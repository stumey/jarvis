using Microsoft.Extensions.DependencyInjection;

namespace Shared.Validation;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers EventSchemaValidator as a singleton with schema loaded from embedded resource.
    /// </summary>
    public static IServiceCollection AddEventSchemaValidation(this IServiceCollection services)
    {
        var schema = EventSchemaLoader.LoadFromEmbeddedResource();
        services.AddSingleton(new EventSchemaValidator(schema));
        return services;
    }

    /// <summary>
    /// Registers EventSchemaValidator as a singleton with schema loaded from a file path.
    /// </summary>
    public static IServiceCollection AddEventSchemaValidation(this IServiceCollection services, string schemaFilePath)
    {
        var schema = EventSchemaLoader.LoadFromFile(schemaFilePath);
        services.AddSingleton(new EventSchemaValidator(schema));
        return services;
    }
}
