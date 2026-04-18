using System.IdentityModel.Tokens.Jwt;

namespace CineTrack.Application.Features.Auth.Common;

public static class AuthTokenClaimNames
{
    public const string TokenType = "token_type";
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string Email = JwtRegisteredClaimNames.Email;
    public const string TokenId = JwtRegisteredClaimNames.Jti;
}
