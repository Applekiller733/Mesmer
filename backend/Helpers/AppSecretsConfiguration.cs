using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;

namespace SongAppApi.Helpers
{
    public static class AppSecretsConfiguration
    {
        // When APP_SECRETS_ARN is set (in AWS), fetch the JSON secret from Secrets
        // Manager and merge its key/values into configuration. The secret holds the
        // JWT signing secret and SMTP credentials under config-style keys
        // ("AppSettings:Secret", "AppSettings:SmtpHost", ...), so AppSettings binds
        // to them. No-op locally (values come from .env / appsettings instead).
        public static void AddAppSecrets(this IConfigurationManager configuration)
        {
            var secretId = configuration["APP_SECRETS_ARN"];
            if (string.IsNullOrWhiteSpace(secretId))
                return;

            var region = RegionEndpoint.GetBySystemName(
                configuration["AWS_REGION"] ?? "eu-north-1");

            using var client = new AmazonSecretsManagerClient(region);
            var response = client
                .GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId })
                .GetAwaiter().GetResult();

            if (string.IsNullOrWhiteSpace(response.SecretString))
                return;

            var values = JsonSerializer.Deserialize<Dictionary<string, string?>>(response.SecretString);
            if (values is not null)
                configuration.AddInMemoryCollection(values);
        }
    }
}
