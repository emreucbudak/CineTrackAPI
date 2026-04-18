namespace CineTrack.Application.Features.Auth.Common;

public static class AuthTokenTypes
{
    public const string Access = "access";
    public const string PendingLogin = "pending-login";
    public const string PendingRegister = "pending-register";
    public const string PendingPasswordReset = "pending-password-reset";
    public const string VerifyLogin = PendingLogin;
    public const string LoginVerification = PendingLogin;
    public const string VerifyRegister = PendingRegister;
    public const string RegisterVerification = PendingRegister;
    public const string ForgotPassword = PendingPasswordReset;
    public const string PasswordResetVerification = PendingPasswordReset;
}
