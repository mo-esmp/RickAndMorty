using Domain.Characters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Infrastructure.DataPersistence;

public class ApplicationDbContext : DbContext
{
    protected ApplicationDbContext(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    protected IConfiguration Configuration { get; }

    public DbSet<Character> Characters => Set<Character>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    protected void ApplyConfiguration(ModelBuilder modelBuilder, string[] namespaces)
    {
        var methodInfo = (typeof(ModelBuilder).GetMethods()).Single((e =>
            e.Name == "ApplyConfiguration" &&
            e.ContainsGenericParameters &&
            e.GetParameters().SingleOrDefault()?.ParameterType.GetGenericTypeDefinition() ==
            typeof(IEntityTypeConfiguration<>)));

        foreach (var configType in typeof(ApplicationDbContext)
                     .GetTypeInfo().Assembly
                     .GetTypes()
                     .Where(t => t.Namespace != null &&
                                 namespaces.Any(n => n == t.Namespace) &&
                                 t.GetInterfaces().Any(i => i.IsGenericType &&
                                                            i.GetGenericTypeDefinition() ==
                                                            typeof(IEntityTypeConfiguration<>)
                                 )
                     )
                )
        {
            var type = configType.GetInterfaces().First();
            methodInfo.MakeGenericMethod(type.GenericTypeArguments[0]).Invoke(modelBuilder, [Activator.CreateInstance(configType)]);
        }
    }
}