using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;

namespace CBIR.Data
{
    public class ImagesDbContextFactory : IDesignTimeDbContextFactory<ImagesDbContext>
    {
        public ImagesDbContext CreateDbContext(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env}.json", true, true)
                .Build();

            var optBuilder = new DbContextOptionsBuilder<ImagesDbContext>();
            optBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            
            var dbContext = new ImagesDbContext(optBuilder.Options);

            return dbContext;
        }
    }
}
