using System.Text.Json;
using BackendASPNET.Data;
using BackendASPNET.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendASPNET.Controllers;

[ApiController]
[Route("api/v1/todos")]
public class TodosController(AppDb db, AuthSupport auth) : ControllerBase
{
    private async Task<Todo> Find(long id) =>
        await db.Todos.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id)
        ?? throw new ApiException(404, "Todo not found");

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await auth.Require(Request, db);
        var todos = await db.Todos.Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Api.Ok("Todos", Api.Data("todos", todos));
    }

    [HttpGet("current-user")]
    public async Task<IActionResult> CurrentUser()
    {
        var user = await auth.Require(Request, db);
        var todos = await db.Todos.Include(t => t.User)
            .Where(t => t.UserId == user.Id)
            .OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Api.Ok("Todos", Api.Data("todos", todos));
    }

    [HttpPost]
    public async Task<IActionResult> Store([FromBody] Dictionary<string, JsonElement> body)
    {
        var user = await auth.Require(Request, db);
        var title = body.GetValueOrDefault("title").ValueKind == JsonValueKind.String
            ? body["title"].GetString()
            : null;
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ApiException(400, "Title is required");
        }

        var todo = new Todo
        {
            Title = title,
            Description = body.GetValueOrDefault("description").ValueKind == JsonValueKind.String
                ? body["description"].GetString() ?? ""
                : "",
            User = user,
        };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return Api.Created("Todo created successfully", Api.Data("todo", todo));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Show(long id)
    {
        await auth.Require(Request, db);
        return Api.Ok("Todo", Api.Data("todo", await Find(id)));
    }

    [HttpPatch("{id:long}/toggle")]
    public async Task<IActionResult> Toggle(long id)
    {
        await auth.Require(Request, db);
        var todo = await Find(id);
        todo.IsCompleted = !todo.IsCompleted;
        await db.SaveChangesAsync();
        return Api.Ok("Todo toggled", Api.Data("todo", todo));
    }

    [HttpPatch("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Dictionary<string, JsonElement> body)
    {
        await auth.Require(Request, db);
        var todo = await Find(id);
        if (body.TryGetValue("title", out var title) && title.ValueKind == JsonValueKind.String)
        {
            todo.Title = title.GetString()!;
        }
        if (body.TryGetValue("description", out var description)
            && description.ValueKind == JsonValueKind.String)
        {
            todo.Description = description.GetString()!;
        }
        if (body.TryGetValue("is_completed", out var completed)
            && completed.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            todo.IsCompleted = completed.GetBoolean();
        }
        await db.SaveChangesAsync();
        return Api.Ok("Todo updated", Api.Data("todo", todo));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Destroy(long id)
    {
        await auth.Require(Request, db);
        db.Todos.Remove(await Find(id));
        await db.SaveChangesAsync();
        return Api.Ok("Todo deleted successfully");
    }
}
