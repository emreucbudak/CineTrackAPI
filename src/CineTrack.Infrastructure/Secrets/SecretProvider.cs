namespace CineTrack.Infrastructure.Secrets;

public static class SecretProvider
{
    private const string SecretsBasePath = "/run/secrets";

    public static string GetSecret(string secretName)
    {
        var secretPath = Path.Combine(SecretsBasePath, secretName);

        if (!File.Exists(secretPath))
            throw new FileNotFoundException(
                $"Secret '{secretName}' not found at path '{secretPath}'.");

        return File.ReadAllText(secretPath).Trim();
    }

    public static string GetDatabaseConnectionString() =>
        GetSecret("db_connection_string");

    public static string GetRabbitMqConnectionString() =>
        GetSecret("rabbitmq_connection_string");
}
