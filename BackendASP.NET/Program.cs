using System.Text.Json;
using System.Text.Json.Serialization;
using BackendASPNET.Data;
using BackendASPNET.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var dbFile = Environment.GetEnvironmentVariable("DB_FILE") ?? "db.sqlite";
builder.Services.AddDbContext<AppDb>(options => options.UseSqlite($"Data Source={dbFile}"));
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<AuthSupport>();
builder.Services.AddCors();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // The shared CLIuno contract serialises entity fields as snake_case;
    // envelope keys are dictionary keys and stay exactly as written.
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();
}

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

// Turn ApiException into the shared error envelope
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ApiException e)
    {
        context.Response.StatusCode = e.Status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            new Dictionary<string, object?> { ["status"] = "error", ["message"] = e.Message });
    }
});

// Uploaded images are public and unversioned — no /api/v1 prefix, no auth.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(BackendASPNET.Controllers.UploadsController.EnsureUploadDir()),
    RequestPath = "/uploads",
});

app.MapControllers();
app.MapGet("/", () => new { status = "success", message = "CLIuno ASP.NET API" });

app.Run();
