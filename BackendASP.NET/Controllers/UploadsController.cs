using BackendASPNET.Data;
using BackendASPNET.Support;
using Microsoft.AspNetCore.Mvc;

namespace BackendASPNET.Controllers;

[ApiController]
[Route("api/v1/uploads")]
public class UploadsController(AppDb db, AuthSupport auth) : ControllerBase
{
    /// <summary>5 MB — matches every other CLIuno backend template.</summary>
    public const long MaxUploadBytes = 5L * 1024 * 1024;

    /// <summary>Uploaded files live at the repo root and are served back from /uploads.</summary>
    public static string UploadDir => Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    /// <summary>The extension comes from the detected mime, never the client's filename.</summary>
    private static readonly Dictionary<string, string> AllowedImageTypes = new()
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    public static string EnsureUploadDir()
    {
        Directory.CreateDirectory(UploadDir);
        return UploadDir;
    }

    /// <summary>
    /// Stores the bytes and hands back a URL. Attaching it to a user or a post is the
    /// caller's job (PATCH /users/current, POST /posts) — upload mutates nothing.
    /// </summary>
    [HttpPost("image")]
    public async Task<IActionResult> Image([FromForm] IFormFile? file)
    {
        await auth.Require(Request, db);

        if (file is null || file.Length == 0)
        {
            throw new ApiException(400, "No image file provided");
        }

        if (!AllowedImageTypes.TryGetValue(file.ContentType, out var extension))
        {
            throw new ApiException(400, $"Unsupported image type: {file.ContentType}");
        }

        if (file.Length > MaxUploadBytes)
        {
            throw new ApiException(413, "Image exceeds the 5 MB limit");
        }

        var filename = $"{Guid.NewGuid()}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(EnsureUploadDir(), filename)))
        {
            await file.CopyToAsync(stream);
        }

        // Absolute so a frontend on another origin can use it straight as an image source.
        var configured = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");
        var baseUrl = string.IsNullOrWhiteSpace(configured)
            ? $"{Request.Scheme}://{Request.Host}"
            : configured.TrimEnd('/');

        return Api.Created(
            "Image uploaded",
            Api.Data("url", $"{baseUrl}/uploads/{filename}", "filename", filename));
    }
}
