using DevTrack.Application.Interfaces;
using DevTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevTrack.Tests.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionVariable = "DEVTRACK_TEST_CONNECTION";
    private const string DefaultLocalDbConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=DevTrackTestDb;Trusted_Connection=True;MultipleActiveResultSets=true";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        var connectionString = GetTestConnectionString();

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DevTrackDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<DevTrackDbContext>(options => options.UseSqlServer(connectionString));
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, passwordHasher);
    }

    private static string GetTestConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(TestConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = DefaultLocalDbConnection;


        var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (!database.Contains("Test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Refusing to run integration tests against database '{database}'. " +
                "The test suite deletes and recreates its database, so the name must contain 'Test'.");

        return connectionString;
    }
}