using Microsoft.Extensions.DependencyInjection;

namespace Shared.Data;

public static class DataServiceExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(connectionString));
        services.AddSingleton<IEventRepository, EventRepository>();
        services.AddSingleton<IShoppingListRepository, ShoppingListRepository>();
        services.AddSingleton<IShoppingItemRepository, ShoppingItemRepository>();
        return services;
    }
}
