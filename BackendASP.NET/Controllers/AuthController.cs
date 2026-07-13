using System.Security.Cryptography;
using BackendASPNET.Data;
using BackendASPNET.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendASPNET.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AppDb db, JwtService jwt, AuthSupport auth) : ControllerBase
{
    private static string RandomToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(20)).ToLowerInvariant();

    private async Task<Dictionary<string, object?>> TokensFor(User user)
    {
        var token = jwt.Sign(user.Id, TimeSpan.FromHours(1), refresh: false);
        var refreshToken = jwt.Sign(user.Id, TimeSpan.FromDays(7), refresh: true);
        user.RefreshToken = refreshToken;
        await db.SaveChangesAsync();
        return Api.Data("user", user, "token", token, "refreshToken", refreshToken);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Dictionary<string, string?> body)
    {
        var username = body.GetValueOrDefault("username");
        var email = body.GetValueOrDefault("email");
        var password = body.GetValueOrDefault("password");
        if (username is null || email is null || password is null
            || body.GetValueOrDefault("first_name") is null
            || body.GetValueOrDefault("last_name") is null)
        {
            throw new ApiException(400, "Missing required fields");
        }
        if (password.Length < 8)
        {
            throw new ApiException(400, "Password must be at least 8 characters");
        }
        if (await db.Users.AnyAsync(u => u.Username == username || u.Email == email))
        {
            throw new ApiException(400, "User already exists");
        }

        // Default role is created on first use so a fresh install works out of the box
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == "user");
        if (role is null)
        {
            role = new Role { Name = "user" };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        var user = new User
        {
            Username = username,
            FirstName = body["first_name"]!,
            LastName = body["last_name"]!,
            Email = email,
            Phone = body.GetValueOrDefault("phone"),
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            // The verify token is stored so verify-email can look the user up by token later.
            VerifyToken = RandomToken(),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        // In production, email the token; templates keep it local to the database.

        return Api.Created(
            "User created successfully and an email has been sent to you for verification",
            Api.Data("user", user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Dictionary<string, string?> body)
    {
        var login = body.GetValueOrDefault("usernameOrEmail");
        var password = body.GetValueOrDefault("password");
        if (login is null || password is null)
        {
            throw new ApiException(400, "usernameOrEmail and password are required");
        }

        var user = login.Contains('@')
            ? await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == login)
            : await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == login);
        if (user is null || user.IsDeleted || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            throw new ApiException(401, "Invalid username/email or password");
        }

        user.IsOnline = true;
        return Api.Ok("Login successful", await TokensFor(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var user = await auth.Require(Request, db);
        user.IsOnline = false;
        user.RefreshToken = null;
        await db.SaveChangesAsync();
        return Api.Ok("Logout successful");
    }

    [HttpPost("check-token")]
    public async Task<IActionResult> CheckToken()
    {
        var user = await auth.Require(Request, db);
        return Api.Ok("Token is valid", Api.Data("user", user));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] Dictionary<string, string?>? body)
    {
        // Frontends send the refresh token in the body; the header is a fallback
        var raw = body?.GetValueOrDefault("refreshToken") ?? auth.Bearer(Request);
        var userId = raw is null ? null : jwt.Verify(raw, refresh: true);
        var user = userId is null ? null : await db.Users.FindAsync(userId);
        if (user is null || user.RefreshToken != raw)
        {
            throw new ApiException(401, "Invalid or expired refresh token");
        }

        var data = await TokensFor(user);
        data.Remove("user");
        return Api.Ok("Token refreshed successfully", data);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] Dictionary<string, string?> body)
    {
        var user = await auth.Require(Request, db);
        var oldPassword = body.GetValueOrDefault("oldPassword")
            ?? body.GetValueOrDefault("current_password");
        var newPassword = body.GetValueOrDefault("newPassword")
            ?? body.GetValueOrDefault("new_password");
        if (oldPassword is null || newPassword is null || newPassword.Length < 8)
        {
            throw new ApiException(400, "oldPassword and newPassword are required");
        }
        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
        {
            throw new ApiException(400, "Current password is incorrect");
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await db.SaveChangesAsync();
        return Api.Ok("Password changed successfully");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] Dictionary<string, string?> body)
    {
        // Frontends send `email`; `usernameOrEmail` is kept for compatibility
        var login = body.GetValueOrDefault("email") ?? body.GetValueOrDefault("usernameOrEmail");
        if (login is null)
        {
            throw new ApiException(400, "Email is required");
        }

        var user = login.Contains('@')
            ? await db.Users.FirstOrDefaultAsync(u => u.Email == login)
            : await db.Users.FirstOrDefaultAsync(u => u.Username == login);
        if (user is not null)
        {
            user.ResetToken = RandomToken();
            await db.SaveChangesAsync();
            // In production, email the token; templates keep it local to the database.
        }
        return Api.Ok("If the email exists, a reset link has been sent");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] Dictionary<string, string?> body)
    {
        var token = body.GetValueOrDefault("token");
        var password = body.GetValueOrDefault("password");
        var user = token is null
            ? null
            : await db.Users.FirstOrDefaultAsync(u => u.ResetToken == token);
        if (user is null)
        {
            throw new ApiException(400, "Invalid or expired reset token");
        }
        if (password is null || password.Length < 8)
        {
            throw new ApiException(400, "Password must be at least 8 characters");
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(password);
        user.ResetToken = null;
        await db.SaveChangesAsync();
        return Api.Ok("Password reset successful");
    }

    [HttpPost("send-verify-email")]
    public async Task<IActionResult> SendVerifyEmail()
    {
        var user = await auth.Require(Request, db);
        user.VerifyToken = RandomToken();
        await db.SaveChangesAsync();
        // In production, email the token; templates keep it local to the database.
        return Api.Ok("Email sent successfully");
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] Dictionary<string, string?> body)
    {
        var token = body.GetValueOrDefault("token");
        var user = token is null
            ? null
            : await db.Users.FirstOrDefaultAsync(u => u.VerifyToken == token);
        if (user is null)
        {
            throw new ApiException(400, "Invalid or expired verification token");
        }
        user.IsVerified = true;
        user.VerifyToken = null;
        await db.SaveChangesAsync();
        return Api.Ok("Email verified successfully");
    }

    // OTP endpoints act on the authenticated user (RFC 6238 TOTP)
    [HttpPost("otp/generate")]
    public async Task<IActionResult> OtpGenerate()
    {
        var user = await auth.Require(Request, db);
        var secret = TotpService.GenerateSecret();
        user.OtpBase32 = secret;
        user.OtpAuthUrl = TotpService.ProvisioningUri(secret, user.Username);
        user.IsOtpEnabled = false;
        await db.SaveChangesAsync();
        return Api.Ok(
            "OTP secret generated",
            Api.Data("secret", secret, "base32", secret, "otpauth_url", user.OtpAuthUrl));
    }

    [HttpPost("otp/verify")]
    public async Task<IActionResult> OtpVerify([FromBody] Dictionary<string, string?> body)
    {
        var user = await auth.Require(Request, db);
        if (user.OtpBase32 is null)
        {
            throw new ApiException(400, "OTP is not set up");
        }
        var code = body.GetValueOrDefault("otp") ?? body.GetValueOrDefault("token");
        if (!TotpService.Verify(user.OtpBase32, code))
        {
            throw new ApiException(401, "Invalid OTP code");
        }
        user.IsOtpEnabled = true;
        await db.SaveChangesAsync();
        return Api.Ok("OTP verified successfully");
    }

    [HttpPost("otp/validate")]
    public async Task<IActionResult> OtpValidate([FromBody] Dictionary<string, string?> body)
    {
        var user = await auth.Require(Request, db);
        if (user.OtpBase32 is null || !user.IsOtpEnabled)
        {
            throw new ApiException(400, "OTP is not enabled for this user");
        }
        var code = body.GetValueOrDefault("otp") ?? body.GetValueOrDefault("token");
        if (!TotpService.Verify(user.OtpBase32, code))
        {
            throw new ApiException(401, "Invalid OTP code");
        }
        return Api.Ok("Token is valid");
    }

    [HttpPost("otp/disable")]
    public async Task<IActionResult> OtpDisable()
    {
        var user = await auth.Require(Request, db);
        user.IsOtpEnabled = false;
        user.OtpBase32 = null;
        user.OtpAuthUrl = null;
        await db.SaveChangesAsync();
        return Api.Ok("OTP disabled successfully");
    }
}
