using Infrastructure.DataPersistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace WebApp.Tests.TestSetup;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("TestDatabase")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7.0")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSqlConnection"] = _postgresContainer.GetConnectionString(),
                    ["ConnectionStrings:RedisConnection"] = _redisContainer.GetConnectionString()
                });
        });
    }

    public async Task InitializeAsync()
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
        await _postgresContainer.StartAsync(cts.Token);
        await _redisContainer.StartAsync(cts.Token);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    /// <summary>
    /// Resets the database and cache to a clean state before each test.
    /// This ensures complete test isolation.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using IServiceScope scope = Services.CreateScope();

        // Reset Database
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Use TRUNCATE for better performance (faster than EnsureDeleted/EnsureCreated)
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Characters\" RESTART IDENTITY CASCADE");

        // Clear any cached entities in the DbContext
        context.ChangeTracker.Clear();

        // Reset Redis Cache
        await ResetCacheAsync();
    }

    /// <summary>
    /// Clears all keys from the Redis cache.
    /// </summary>
    public async Task ResetCacheAsync()
    {
        try
        {
            var redis = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString(), options => options.AllowAdmin = true);
            var server = redis.GetServer(redis.GetEndPoints().First());
            await server.FlushDatabaseAsync();
            await redis.CloseAsync();
        }
        catch (Exception)
        {
            // If Redis reset fails, continue (tests might not always use cache)
            // In production tests, you might want to throw here
        }
    }
}