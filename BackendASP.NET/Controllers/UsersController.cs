using BackendASPNET.Data;
using BackendASPNET.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendASPNET.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController(AppDb db, AuthSupport auth) : ControllerBase
{
    private async Task<User> Find(long id) =>
        await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id)
        ?? throw new ApiException(404, "User not found");

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await auth.Require(Request, db);
        return Api.Ok("Users", Api.Data("users", await db.Users.Include(u => u.Role).ToListAsync()));
    }

    [HttpGet("current")]
    public async Task<IActionResult> Current() =>
        Api.Ok("Current user", Api.Data("user", await auth.Require(Request, db)));

    [HttpPatch("current")]
    public async Task<IActionResult> UpdateCurrent([FromBody] Dictionary<string, string?> body)
    {
        var user = await auth.Require(Request, db);
        Apply(user, body);
        await db.SaveChangesAsync();
        return Api.Ok("User updated", Api.Data("user", user));
    }

    [HttpDelete("current")]
    public async Task<IActionResult> DeleteCurrent()
    {
        var user = await auth.Require(Request, db);
        user.IsDeleted = true;
        await db.SaveChangesAsync();
        return Api.Ok("User deleted", Api.Data("user", user));
    }

    [HttpGet("username/{username}")]
    public async Task<IActionResult> ByUsername(string username)
    {
        await auth.Require(Request, db);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username)
            ?? throw new ApiException(404, "User not found");
        return Api.Ok("User", Api.Data("user", user));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Show(long id)
    {
        await auth.Require(Request, db);
        return Api.Ok("User", Api.Data("user", await Find(id)));
    }

    [HttpGet("{id:long}/posts")]
    public async Task<IActionResult> UserPosts(long id)
    {
        await auth.Require(Request, db);
        await Find(id);
        var posts = await db.Posts
            .Include(p => p.User)
            .Include(p => p.Comments)
            .ThenInclude(c => c.User)
            .Where(p => p.UserId == id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Api.Ok("Posts", Api.Data("posts", posts));
    }

    [HttpGet("{id:long}/roles")]
    public async Task<IActionResult> UserRoles(long id)
    {
        await auth.Require(Request, db);
        return Api.Ok("Role found", Api.Data("role", (await Find(id)).Role));
    }

    [HttpPatch("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Dictionary<string, string?> body)
    {
        AuthSupport.RequireAdmin(await auth.Require(Request, db));
        var user = await Find(id);
        Apply(user, body);
        await db.SaveChangesAsync();
        return Api.Ok("User updated", Api.Data("user", user));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Destroy(long id)
    {
        AuthSupport.RequireAdmin(await auth.Require(Request, db));
        var user = await Find(id);
        user.IsDeleted = true;
        await db.SaveChangesAsync();
        return Api.Ok("User deleted", Api.Data("user", user));
    }

    private static void Apply(User user, Dictionary<string, string?> body)
    {
        if (body.GetValueOrDefault("first_name") is { } firstName)
        {
            user.FirstName = firstName;
        }
        if (body.GetValueOrDefault("last_name") is { } lastName)
        {
            user.LastName = lastName;
        }
        if (body.GetValueOrDefault("phone") is { } phone)
        {
            user.Phone = phone;
        }
        if (body.GetValueOrDefault("avatar_url") is { } avatarUrl)
        {
            user.AvatarUrl = avatarUrl;
        }
    }
}
