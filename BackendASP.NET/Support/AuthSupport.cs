using BackendASPNET.Data;
using Microsoft.EntityFrameworkCore;

namespace BackendASPNET.Support;

public class AuthSupport(JwtService jwt)
{
    public string? Bearer(HttpRequest request)
    {
        string? header = request.Headers.Authorization;
        return header is not null && header.StartsWith("Bearer ") ? header[7..] : null;
    }

    /// <summary>Resolve the Bearer user or throw the 401 the middleware turns into an envelope.</summary>
    public async Task<User> Require(HttpRequest request, AppDb db)
    {
        var token = Bearer(request);
        var userId = token is null ? null : jwt.Verify(token, refresh: false);
        var user = userId is null
            ? null
            : await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        return user ?? throw new ApiException(401, "No token provided or token is invalid");
    }

    public static void RequireAdmin(User user)
    {
        if (user.Role?.Name != "admin")
        {
            throw new ApiException(403, "Forbidden: Permission denied");
        }
    }
}
