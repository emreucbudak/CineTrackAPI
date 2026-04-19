namespace CineTrack.Infrastructure.Email;

public class MailtrapEmailApiSettings
{
    public string ApiBaseUrl { get; set; } = "https://send.api.mailtrap.io/api";
    public string ApiToken { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@your-verified-domain.com";
    public string FromName { get; set; } = "CineTrack";
}
