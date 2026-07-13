using OtpNet;

namespace BackendASPNET.Support;

/// <summary>RFC 6238 TOTP (sha1, 6 digits, 30s) via Otp.NET.</summary>
public static class TotpService
{
    public static string GenerateSecret() =>
        Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(15))[..24];

    public static string ProvisioningUri(string secret, string label) =>
        $"otpauth://totp/CLIuno:{label}?secret={secret}&issuer=CLIuno&algorithm=SHA1&digits=6&period=30";

    public static bool Verify(string secret, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(1, 1));
    }
}
