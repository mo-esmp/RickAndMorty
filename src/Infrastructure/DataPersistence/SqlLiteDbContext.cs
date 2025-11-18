using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DataPersistence;

internal class SqlLiteDbContext(IConfiguration configuration) : ApplicationDbContext(configuration)
{
    private const string Namespace = "Infrastructure.DataPersistence.Configurations";

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite(Configuration.GetConnectionString("SqlLiteConnection"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var namespaces = new[] { Namespace, $"{Namespace}.SqlLite" };
        ApplyConfiguration(modelBuilder, namespaces);
    }
}