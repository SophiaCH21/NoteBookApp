using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NoteManagerApi.Data;

namespace NoteManagerApi.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Любая валидная строка для Npgsql; EF Tools важен провайдер
            var conn = Environment.GetEnvironmentVariable("PG_CONNECTION")
                      ?? "Host=localhost;Port=5432;Database=tmp;Username=postgres;Password=postgres;SSL Mode=Require;Trust Server Certificate=true";

            optionsBuilder
                .UseNpgsql(conn)
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging();

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
