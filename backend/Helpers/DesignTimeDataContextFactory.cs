using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SongAppApi.Helpers
{
    // Used by `dotnet ef` (migrations / scaffolding). It builds the DataContext the
    // same way the running app does — Aurora over IAM auth when DB_HOST is set,
    // otherwise the local connection string — reading only process environment
    // variables and appsettings.json. Having this factory makes `dotnet ef`
    // deterministic instead of depending on how it resolves the web host, and it
    // does NOT load a local .env, so a DB_HOST you export in the shell always wins.
    public class DesignTimeDataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var auroraDataSource = DbConnection.BuildAuroraDataSource(config);
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            DbConnection.Apply(optionsBuilder, config, auroraDataSource);
            return new DataContext(optionsBuilder.Options);
        }
    }
}
