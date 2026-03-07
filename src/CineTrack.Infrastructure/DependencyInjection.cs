using CineTrack.Infrastructure.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace CineTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        var dbConnectionString = SecretProvider.GetDatabaseConnectionString();
        var rabbitMqConnectionString = SecretProvider.GetRabbitMqConnectionString();

        services.AddCap(options =>
        {
            options.UsePostgreSql(dbConnectionString);

            options.UseRabbitMQ(rabbitMq =>
            {
                rabbitMq.ConnectionFactoryOptions = factory =>
                {
                    factory.Uri = new Uri(rabbitMqConnectionString);
                };
            });

            options.UseDashboard(dashboard =>
            {
                dashboard.PathMatch = "/cap-dashboard";
            });

            options.SucceedMessageExpiredAfter = 24 * 3600;  // 24 saat
            options.FailedMessageExpiredAfter = 30 * 24 * 3600; // 30 gün
            options.FailedRetryCount = 5;
            options.FailedRetryInterval = 60; // 60 saniye
        });

        return services;
    }
}
