using CineTrack.API.Middleware;
using CineTrack.Application;
using CineTrack.Infrastructure;
using CineTrack.Infrastructure.Secrets;
using CineTrack.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("CineTrack API starting up...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var dbConnectionString = SecretProvider.GetDatabaseConnectionString();

    builder.Services.AddApplication();
    builder.Services.AddPersistence(dbConnectionString);
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
