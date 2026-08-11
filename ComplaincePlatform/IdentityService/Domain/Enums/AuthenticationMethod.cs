namespace IdentityService.Domain.Enums;

public enum AuthenticationMethod
{
    Password = 0,
    MfaTotp = 1,
    MfaEmailOtp = 2,
    MfaSmsOtp = 3,
    RefreshToken = 4
}
