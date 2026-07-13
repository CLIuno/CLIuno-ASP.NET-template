using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace BackendASPNET.Data;

public class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public List<User> Users { get; set; } = [];
}

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }

    [JsonIgnore]
    public string Password { get; set; } = "";

    public bool IsOnline { get; set; }
    public bool IsVerified { get; set; }
    public bool IsOtpEnabled { get; set; }

    [JsonIgnore]
    public string? OtpBase32 { get; set; }

    [JsonIgnore]
    public string? OtpAuthUrl { get; set; }

    [JsonIgnore]
    public string? RefreshToken { get; set; }

    [JsonIgnore]
    public string? ResetToken { get; set; }

    [JsonIgnore]
    public string? VerifyToken { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public long? RoleId { get; set; }
    public Role? Role { get; set; }

    [JsonIgnore]
    public List<Todo> Todos { get; set; } = [];

    [JsonIgnore]
    public List<Post> Posts { get; set; } = [];
}

public class Todo
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public long UserId { get; set; }
    public User? User { get; set; }
}

public class Post
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public long UserId { get; set; }
    public User? User { get; set; }

    public List<Comment> Comments { get; set; } = [];
}

public class Comment
{
    public long Id { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public long UserId { get; set; }
    public User? User { get; set; }

    public long PostId { get; set; }

    [JsonIgnore]
    public Post? Post { get; set; }
}

public class Follow
{
    public long Id { get; set; }
    public long FollowerId { get; set; }
    public User? Follower { get; set; }
    public long FollowingId { get; set; }
    public User? Following { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Todo> Todos => Set<Todo>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Follow> Follows => Set<Follow>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        builder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        builder.Entity<Follow>()
            .HasIndex(f => new { f.FollowerId, f.FollowingId })
            .IsUnique();
        builder.Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany()
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Follow>()
            .HasOne(f => f.Following)
            .WithMany()
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
