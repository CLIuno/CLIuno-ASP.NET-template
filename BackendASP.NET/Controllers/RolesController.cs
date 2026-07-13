using BackendASPNET.Data;
using BackendASPNET.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendASPNET.Controllers;

[ApiController]
[Route("api/v1/roles")]
public class RolesController(AppDb db, AuthSupport auth) : ControllerBase
{
    private async Task<User> Admin()
    {
        var user = await auth.Require(Request, db);
        AuthSupport.RequireAdmin(user);
        return user;
    }

    private async Task<Role> Find(long id) =>
        await db.Roles.FindAsync(id) ?? throw new ApiException(404, "Role not found");

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await Admin();
        return Api.Ok("Roles", Api.Data("roles", await db.Roles.ToListAsync()));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Show(long id)
    {
        await Admin();
        return Api.Ok("Role", Api.Data("role", await Find(id)));
    }

    [HttpPost]
    public async Task<IActionResult> Store([FromBody] Dictionary<string, string?> body)
    {
        await Admin();
        var name = body.GetValueOrDefault("name");
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApiException(400, "Role name is required");
        }
        if (await db.Roles.AnyAsync(r => r.Name == name))
        {
            throw new ApiException(400, "Role already exists");
        }
        var role = new Role { Name = name };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return Api.Created("Role created successfully", Api.Data("role", role));
    }

    [HttpPatch("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Dictionary<string, string?> body)
    {
        await Admin();
        var role = await Find(id);
        if (body.GetValueOrDefault("name") is { } name)
        {
            role.Name = name;
        }
        await db.SaveChangesAsync();
        return Api.Ok("Role updated", Api.Data("role", role));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Destroy(long id)
    {
        await Admin();
        db.Roles.Remove(await Find(id));
        await db.SaveChangesAsync();
        return Api.Ok("Role deleted successfully");
    }
}
