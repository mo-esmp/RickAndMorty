using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DataPersistence;

internal class PostgreSqlDbContext(IConfiguration configuration) : ApplicationDbContext(configuration)
{
    private const string Namespace = "Infrastructure.DataPersistence.Configurations";

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(Configuration.GetConnectionString("PostgreSqlConnection"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var namespaces = new[] { Namespace, $"{Namespace}.PostgreSql" };
        ApplyConfiguration(modelBuilder, namespaces);
    }
}