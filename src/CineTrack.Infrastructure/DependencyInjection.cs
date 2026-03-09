using System.Net;
using System.Text;
using CineTrack.Application.Abstractions;
using CineTrack.Infrastructure.Auth;
using CineTrack.Infrastructure.Caching;
using CineTrack.Infrastructure.Consumers;
using CineTrack.Infrastructure.Email;
using CineTrack.Infrastructure.Secrets;
using CineTrack.Infrastructure.Tmdb;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;

namespace CineTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConnectionString = SecretProvider.GetDatabaseConnectionString();
        var rabbitMqConnectionString = SecretProvider.GetRabbitMqConnectionString();
        var tmdbApiKey = SecretProvider.GetTmdbApiKey();

        // CAP (Outbox + RabbitMQ)
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

            options.SucceedMessageExpiredAfter = 24 * 3600;
            options.FailedMessageExpiredAfter = 30 * 24 * 3600;
            options.FailedRetryCount = 5;
            options.FailedRetryInterval = 60;
        });

        // Auth
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtProvider, JwtProvider>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!))
                };
            });

        services.AddAuthorization();

        // Redis Cache
        var redisConnectionString = SecretProvider.GetRedisConnectionString();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "CineTrack:";
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Email (SMTP)
        services.Configure<SmtpSettings>(smtpSettings =>
        {
            configuration.GetSection("Smtp").Bind(smtpSettings);
            smtpSettings.Password = SecretProvider.GetSmtpPassword();
        });
        services.AddTransient<IEmailService, SmtpEmailService>();

        // CAP Consumers
        services.AddTransient<EmailConsumer>();

        // TMDb HttpClient + Polly
        services.AddHttpClient<ITmdbService, TmdbService>(client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tmdbApiKey}");
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
