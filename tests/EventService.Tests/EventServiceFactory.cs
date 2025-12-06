using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data;

namespace EventService.Tests;

public class EventServiceFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public EventServiceFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbConnectionFactory));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(_connectionString));
        });
    }
}
