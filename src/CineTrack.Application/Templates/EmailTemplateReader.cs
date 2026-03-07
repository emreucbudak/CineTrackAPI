using System.Reflection;

namespace CineTrack.Application.Templates;

public static class EmailTemplateReader
{
    private static readonly Assembly Assembly = typeof(EmailTemplateReader).Assembly;

    public static string Read(string templateName)
    {
        var resourceName = $"CineTrack.Application.Templates.{templateName}";

        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Email template '{templateName}' not found.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string WelcomeEmail(string username)
    {
        return Read("WelcomeEmail.html")
            .Replace("{{Username}}", username);
    }

    public static string MovieFavoritedEmail(string movieTitle)
    {
        return Read("MovieFavoritedEmail.html")
            .Replace("{{MovieTitle}}", movieTitle);
    }

    public static string ActorFollowedEmail(string actorName)
    {
        return Read("ActorFollowedEmail.html")
            .Replace("{{ActorName}}", actorName);
    }
}
