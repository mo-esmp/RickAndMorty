using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DataPersistence;

internal class SqlLiteDbContext(IConfiguration configuration, ISaveChangesInterceptor saveChangesInterceptor)
    : ApplicationDbContext(configuration)
{
    private const string Namespace = "Infrastructure.DataPersistence.Configurations";

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite(Configuration.GetConnectionString("SqlLiteConnection"));
        options.AddInterceptors(saveChangesInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        string[] namespaces = [Namespace, $"{Namespace}.SqlLite"];
        ApplyConfiguration(modelBuilder, namespaces);
    }
}
