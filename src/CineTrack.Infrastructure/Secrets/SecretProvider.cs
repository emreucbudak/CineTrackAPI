namespace CineTrack.Infrastructure.Secrets;

public static class SecretProvider
{
    private static readonly string SecretsBasePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "secrets"));

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

    public static string GetTmdbApiKey() =>
        GetSecret("tmdb_api_key");

    public static string GetRedisConnectionString() =>
        GetSecret("redis_connection_string");

    public static string GetSmtpPassword() =>
        GetSecret("smtp_password");
}
