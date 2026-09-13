using Amazon;
using Amazon.RDS.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Pgvector;
using Pgvector.Npgsql;

namespace SongAppApi.Helpers
{
    // Single place that decides how the app talks to Postgres, used by both
    // Program.cs (runtime) and the design-time factory (migrations) so they behave
    // identically:
    //   - In AWS (DB_HOST set): Aurora over IAM authentication — an NpgsqlDataSource
    //     that supplies a fresh RDS auth token as the password, refreshed before the
    //     ~15 min expiry, over TLS.
    //   - Locally (no DB_HOST): the ConnectionStrings:SongAppApiDatabase value.
    public static class DbConnection
    {
        // Returns an IAM-auth data source when DB_HOST is configured, else null.
        public static NpgsqlDataSource? BuildAuroraDataSource(IConfiguration config)
        {
            var host = config["DB_HOST"];
            if (string.IsNullOrWhiteSpace(host))
                return null;

            var port = int.TryParse(config["DB_PORT"], out var parsed) ? parsed : 5432;
            var database = config["DB_NAME"] ?? "postgres";
            var user = config["DB_USER"] ?? "postgres";
            var region = RegionEndpoint.GetBySystemName(config["AWS_REGION"] ?? "eu-north-1");

            var connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = database,
                Username = user,
                SslMode = SslMode.Require,       // IAM auth requires TLS
                TrustServerCertificate = true    // learning-grade; harden with VerifyFull + RDS CA
            }.ConnectionString;

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseVector();
            // Local signing op using the ambient AWS credentials (Lambda role in
            // AWS, your profile locally) — no network call.
            dataSourceBuilder.UsePeriodicPasswordProvider(
                (_, _) => new ValueTask<string>(
                    RDSAuthTokenGenerator.GenerateAuthToken(region, host, port, user)),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromSeconds(5));
            return dataSourceBuilder.Build();
        }

        // Apply the right provider to an options builder. Pass the data source from
        // BuildAuroraDataSource (built once and reused), or null for local.
        public static void Apply(DbContextOptionsBuilder options, IConfiguration config,
            NpgsqlDataSource? auroraDataSource)
        {
            if (auroraDataSource != null)
                options.UseNpgsql(auroraDataSource, o => o.UseVector());
            else
                options.UseNpgsql(config.GetConnectionString("SongAppApiDatabase"), o => o.UseVector());
        }
    }
}
