using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence
{
    public class ACHSagaDbContextFactory : IDesignTimeDbContextFactory<ACHSagaDbContext>
    {
        public ACHSagaDbContext CreateDbContext(string[] args)
        {
            // Build configuration to read connection string
            // We assume appsettings.json is in the execution directory or manageable relative path.
            // For design-time, we might hardcode or point to a specific file.
            
            // Note: During 'dotnet ef', the cwd might be the project folder.
            
            var optionsBuilder = new DbContextOptionsBuilder<ACHSagaDbContext>();
            
            // Use a default connection string for migration creation if one isn't easily resolvable,
            // or try to read from environment/local config.
            // For safety in this environment, I'll use a hardcoded dev string or try basic builder.
            
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=AchAiDB;Trusted_Connection=True;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(connectionString);

            return new ACHSagaDbContext(optionsBuilder.Options);
        }
    }
}
