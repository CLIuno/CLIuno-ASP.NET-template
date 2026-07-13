using BackendASPNET.Data;
using BackendASPNET.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendASPNET.Controllers;

[ApiController]
[Route("api/v1/posts")]
public class PostsController(AppDb db, AuthSupport auth) : ControllerBase
{
    private IQueryable<Post> WithRelations() =>
        db.Posts.Include(p => p.User).Include(p => p.Comments).ThenInclude(c => c.User);

    private async Task<Post> Find(long id) =>
        await WithRelations().FirstOrDefaultAsync(p => p.Id == id)
        ?? throw new ApiException(404, "Post not found");

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await auth.Require(Request, db);
        var posts = await WithRelations().OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Api.Ok("Posts", Api.Data("posts", posts));
    }

    [HttpGet("current-user")]
    public async Task<IActionResult> CurrentUser()
    {
        var user = await auth.Require(Request, db);
        var posts = await WithRelations()
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Api.Ok("Posts", Api.Data("posts", posts));
    }

    [HttpPost]
    public async Task<IActionResult> Store([FromBody] Dictionary<string, string?> body)
    {
        var user = await auth.Require(Request, db);
        var title = body.GetValueOrDefault("title");
        var content = body.GetValueOrDefault("content");
        if (title is null || content is null)
        {
            throw new ApiException(400, "Title and Content are required");
        }

        var post = new Post { Title = title, Content = content, User = user };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        return Api.Created("Post created successfully", Api.Data("post", post));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Show(long id)
    {
        await auth.Require(Request, db);
        return Api.Ok("Post", Api.Data("post", await Find(id)));
    }

    [HttpPatch("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] Dictionary<string, string?> body)
    {
        await auth.Require(Request, db);
        var post = await Find(id);
        if (body.GetValueOrDefault("title") is { } title)
        {
            post.Title = title;
        }
        if (body.GetValueOrDefault("content") is { } content)
        {
            post.Content = content;
        }
        if (body.GetValueOrDefault("imageUrl") is { } imageUrl)
        {
            post.ImageUrl = imageUrl;
        }
        await db.SaveChangesAsync();
        return Api.Ok("Post updated", Api.Data("post", post));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Destroy(long id)
    {
        await auth.Require(Request, db);
        db.Posts.Remove(await Find(id));
        await db.SaveChangesAsync();
        return Api.Ok("Post deleted successfully");
    }

    [HttpGet("{id:long}/user")]
    public async Task<IActionResult> Author(long id)
    {
        await auth.Require(Request, db);
        return Api.Ok("User found", Api.Data("user", (await Find(id)).User));
    }

    [HttpGet("{postId:long}/comments")]
    public async Task<IActionResult> Comments(long postId)
    {
        await auth.Require(Request, db);
        var comments = await db.Comments.Include(c => c.User)
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
        return Api.Ok("Comments", Api.Data("comments", comments));
    }

    [HttpPost("{postId:long}/comments")]
    public async Task<IActionResult> StoreComment(
        long postId, [FromBody] Dictionary<string, string?> body)
    {
        var user = await auth.Require(Request, db);
        var content = body.GetValueOrDefault("content");
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ApiException(400, "Content is required");
        }
        var post = await Find(postId);

        var comment = new Comment { Content = content, User = user, Post = post };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return Api.Created("Comment created successfully", Api.Data("comment", comment));
    }

    [HttpPatch("{postId:long}/comments/{id:long}")]
    public async Task<IActionResult> UpdateComment(
        long postId, long id, [FromBody] Dictionary<string, string?> body)
    {
        await auth.Require(Request, db);
        var comment = await db.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new ApiException(404, "Comment not found");
        if (body.GetValueOrDefault("content") is { } content)
        {
            comment.Content = content;
        }
        await db.SaveChangesAsync();
        return Api.Ok("Comment updated", Api.Data("comment", comment));
    }

    [HttpDelete("{postId:long}/comments/{id:long}")]
    public async Task<IActionResult> DestroyComment(long postId, long id)
    {
        await auth.Require(Request, db);
        var comment = await db.Comments.FindAsync(id)
            ?? throw new ApiException(404, "Comment not found");
        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return Api.Ok("Comment deleted successfully");
    }
}
