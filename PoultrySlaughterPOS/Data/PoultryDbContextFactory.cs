using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace PoultrySlaughterPOS.Data
{
    public class PoultryDbContextFactory : IDesignTimeDbContextFactory<PoultryDbContext>
    {
        public PoultryDbContext CreateDbContext(string[] args)
        {
            // Load configuration from appsettings.json to avoid hardcoded connection string
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<PoultryDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new PoultryDbContext(optionsBuilder.Options);
        }
    }

    public class RuntimePoultryDbContextFactory : IDbContextFactory<PoultryDbContext>
    {
        private readonly DbContextOptions<PoultryDbContext> _options;

        public RuntimePoultryDbContextFactory(DbContextOptions<PoultryDbContext> options)
        {
            _options = options;
        }

        public PoultryDbContext CreateDbContext()
        {
            return new PoultryDbContext(_options);
        }
    }
}