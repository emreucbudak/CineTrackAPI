using System.Globalization;
using System.Security.Cryptography;
using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Application.Features.Auth.Common;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<PendingVerificationDto>>
{
    private const int VerificationCodeLength = 6;
    private const int MaxVerificationAttempts = 5;
    private static readonly TimeSpan VerificationLifetime = TimeSpan.FromMinutes(5);

    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IEmailService _emailService;
    private readonly IJwtProvider _jwtProvider;

    public ForgotPasswordCommandHandler(
        IAppDbContext db,
        ICacheService cache,
        IEmailService emailService,
        IJwtProvider jwtProvider)
    {
        _db = db;
        _cache = cache;
        _emailService = emailService;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<PendingVerificationDto>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
        var expiresAtUtc = DateTime.UtcNow.Add(VerificationLifetime);
        var (temporaryToken, _) = _jwtProvider.GenerateTemporaryToken(
            normalizedEmail,
            AuthTokenTypes.PendingPasswordReset,
            expiresAt: expiresAtUtc);
        var verificationCode = GenerateVerificationCode();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        var cacheEntry = new ForgotPasswordCacheEntry
        {
            UserId = user?.Id,
            Email = normalizedEmail,
            VerificationCode = verificationCode,
            ExpiresAtUtc = expiresAtUtc,
            RemainingAttempts = MaxVerificationAttempts,
            IsCodeVerified = false
        };

        await _cache.SetAsync(
            GetCacheKey(temporaryToken),
            cacheEntry,
            VerificationLifetime,
            cancellationToken);

        if (user is not null)
        {
            try
            {
                await _emailService.SendAsync(
                    normalizedEmail,
                    "CineTrack | Şifre sıfırlama kodu",
                    BuildEmailBody(verificationCode, VerificationLifetime),
                    cancellationToken);
            }
            catch
            {
                await _cache.RemoveAsync(GetCacheKey(temporaryToken), cancellationToken);
                throw;
            }
        }

        return new PendingVerificationDto(temporaryToken, expiresAtUtc, normalizedEmail);
    }

    private static string GetCacheKey(string temporaryToken) => $"auth:forgot-password:{temporaryToken}";

    private static string GenerateVerificationCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString($"D{VerificationCodeLength}", CultureInfo.InvariantCulture);

    private static string BuildEmailBody(string verificationCode, TimeSpan lifetime)
    {
        var totalMinutes = (int)lifetime.TotalMinutes;

        return $"""
                <html>
                  <body style="margin:0; padding:24px; background:#0f0f14; font-family:Arial, sans-serif; color:#f5f5f5;">
                    <div style="max-width:560px; margin:0 auto; background:#171821; border:1px solid #2b2d3a; border-radius:18px; overflow:hidden;">
                      <div style="padding:24px 24px 8px;">
                        <div style="display:inline-block; padding:6px 12px; border-radius:999px; background:#2d161a; color:#ff9d9d; font-size:12px; font-weight:700; letter-spacing:0.6px;">
                          ŞİFRE SIFIRLAMA
                        </div>
                        <h2 style="margin:16px 0 10px; font-size:28px; color:#ffffff;">Şifre sıfırlama kodunuz</h2>
                        <p style="margin:0 0 18px; color:#b7bccd; line-height:1.6;">
                          Şifrenizi yenilemek için aşağıdaki 6 haneli kodu kullanın.
                        </p>
                      </div>
                      <div style="padding:0 24px 24px;">
                        <div style="padding:18px 20px; border-radius:16px; background:#10111a; border:1px solid #2f3242; text-align:center;">
                          <div style="font-size:13px; color:#8f96ad; margin-bottom:8px;">Şifre sıfırlama kodu</div>
                          <div style="font-size:34px; font-weight:800; letter-spacing:8px; color:#ffffff;">{verificationCode}</div>
                        </div>
                        <p style="margin:18px 0 8px; color:#b7bccd; line-height:1.6;">
                          Kod {totalMinutes} dakika boyunca geçerlidir. Bu talep size ait değilse hesabınız için herhangi bir değişiklik yapılmaz.
                        </p>
                      </div>
                    </div>
                  </body>
                </html>
                """;
    }
}
