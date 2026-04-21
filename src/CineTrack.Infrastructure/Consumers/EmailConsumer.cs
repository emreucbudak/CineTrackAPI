using CineTrack.Application.Abstractions;
using CineTrack.Application.Events;
using CineTrack.Application.Templates;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace CineTrack.Infrastructure.Consumers;

public class EmailConsumer : ICapSubscribe
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailConsumer> _logger;

    public EmailConsumer(IEmailService emailService, ILogger<EmailConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [CapSubscribe(EventNames.UserRegistered)]
    public async Task HandleUserRegisteredAsync(UserRegisteredEvent @event)
    {
        _logger.LogInformation("Processing welcome email for user {UserId} ({Email})", @event.UserId, @event.Email);

        var html = EmailTemplateReader.WelcomeEmail(@event.Username);

        await _emailService.SendAsync(
            @event.Email,
            "Welcome to CineTrack!",
            html);

        _logger.LogInformation("Welcome email sent to {Email}", @event.Email);
    }
}
