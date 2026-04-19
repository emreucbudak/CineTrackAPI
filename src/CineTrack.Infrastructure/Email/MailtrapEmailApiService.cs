using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using CineTrack.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CineTrack.Infrastructure.Email;

public class MailtrapEmailApiService : IEmailService
{
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly MailtrapEmailApiSettings _settings;
    private readonly ILogger<MailtrapEmailApiService> _logger;

    public MailtrapEmailApiService(
        HttpClient httpClient,
        IOptions<MailtrapEmailApiSettings> settings,
        ILogger<MailtrapEmailApiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var flow = ResolveFlow(subject);
        var payload = new
        {
            from = new
            {
                email = _settings.FromEmail,
                name = _settings.FromName
            },
            to = new[]
            {
                new { email = to }
            },
            subject,
            html = htmlBody,
            text = BuildPlainText(htmlBody),
            category = flow,
            headers = new Dictionary<string, string>
            {
                ["X-CineTrack-Flow"] = flow
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.ApiBaseUrl.TrimEnd('/')}/send");
        request.Headers.Add("Api-Token", _settings.ApiToken);
        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Mailtrap transactional send failed with status {StatusCode}. Response: {ResponseBody}",
                (int)response.StatusCode,
                responseBody);
            throw new InvalidOperationException(
                $"Mailtrap transactional send failed with status {(int)response.StatusCode}.");
        }

        _logger.LogInformation(
            "Mailtrap transactional email sent for flow {Flow} to {Recipient}.",
            flow,
            to);
    }

    private static string ResolveFlow(string subject)
    {
        if (subject.Contains("Giris", StringComparison.OrdinalIgnoreCase) ||
            subject.Contains("Giriş", StringComparison.OrdinalIgnoreCase) ||
            subject.Contains("login", StringComparison.OrdinalIgnoreCase))
        {
            return "auth-login";
        }

        if (subject.Contains("Kayit", StringComparison.OrdinalIgnoreCase) ||
            subject.Contains("Kayıt", StringComparison.OrdinalIgnoreCase) ||
            subject.Contains("register", StringComparison.OrdinalIgnoreCase))
        {
            return "auth-register";
        }

        if (subject.Contains("Sifre", StringComparison.OrdinalIgnoreCase) ||
            subject.Contains("Şifre", StringComparison.OrdinalIgnoreCase) ||
            subject.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            return "auth-forgot-password";
        }

        return "auth-general";
    }

    private static string BuildPlainText(string htmlBody)
    {
        var text = HtmlTagRegex.Replace(htmlBody, " ");
        text = WebUtility.HtmlDecode(text);
        return WhitespaceRegex.Replace(text, " ").Trim();
    }
}
