using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DataPersistence;

internal class PostgreSqlDbContext(IConfiguration configuration, ISaveChangesInterceptor saveChangesInterceptor)
    : ApplicationDbContext(configuration)
{
    private const string Namespace = "Infrastructure.DataPersistence.Configurations";

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(Configuration.GetConnectionString("PostgreSqlConnection"));
        options.AddInterceptors(saveChangesInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        string[] namespaces = [Namespace, $"{Namespace}.PostgreSql"];
        ApplyConfiguration(modelBuilder, namespaces);
    }
}
