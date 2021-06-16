using CBIR.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;

namespace CBIR.Data
{
    public class ImagesDbContext : DbContext
    {
        public DbSet<Image> Images { get; set; }
        public DbSet<Category> Categories { get; set; }

        public ImagesDbContext(DbContextOptions<ImagesDbContext> options) : 
            base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
