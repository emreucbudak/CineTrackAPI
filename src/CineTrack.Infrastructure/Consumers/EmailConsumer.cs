using CineTrack.Application.Abstractions;
using CineTrack.Application.Events;
using CineTrack.Application.Templates;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineTrack.Infrastructure.Consumers;

public class EmailConsumer : ICapSubscribe
{
    private readonly IEmailService _emailService;
    private readonly IAppDbContext _db;
    private readonly ILogger<EmailConsumer> _logger;

    public EmailConsumer(IEmailService emailService, IAppDbContext db, ILogger<EmailConsumer> logger)
    {
        _emailService = emailService;
        _db = db;
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

    [CapSubscribe(EventNames.MovieFavorited)]
    public async Task HandleMovieFavoritedAsync(MovieFavoritedEvent @event)
    {
        _logger.LogInformation("Processing favorite notification for user {UserId}, movie {TmdbId}", @event.UserId, @event.TmdbId);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == @event.UserId);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found, skipping movie favorited email", @event.UserId);
            return;
        }

        var html = EmailTemplateReader.MovieFavoritedEmail(@event.Title);

        await _emailService.SendAsync(
            user.Email,
            $"You favorited: {@event.Title}",
            html);

        _logger.LogInformation("Movie favorited email sent to {Email}", user.Email);
    }

    [CapSubscribe(EventNames.ActorFollowed)]
    public async Task HandleActorFollowedAsync(ActorFollowedEvent @event)
    {
        _logger.LogInformation("Processing follow notification for user {UserId}, actor {TmdbId}", @event.UserId, @event.TmdbId);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == @event.UserId);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found, skipping actor followed email", @event.UserId);
            return;
        }

        var html = EmailTemplateReader.ActorFollowedEmail(@event.Name);

        await _emailService.SendAsync(
            user.Email,
            $"You're now following: {@event.Name}",
            html);

        _logger.LogInformation("Actor followed email sent to {Email}", user.Email);
    }
}
