using BackendASPNET.Data;
using BackendASPNET.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendASPNET.Controllers;

[ApiController]
[Route("api/v1/follows")]
public class FollowsController(AppDb db, AuthSupport auth) : ControllerBase
{
    private async Task<User> Target(long userId) =>
        await db.Users.FindAsync(userId) ?? throw new ApiException(404, "User not found");

    [HttpPost("{userId:long}/follow")]
    public async Task<IActionResult> FollowUser(long userId)
    {
        var me = await auth.Require(Request, db);
        var other = await Target(userId);
        if (me.Id == other.Id)
        {
            throw new ApiException(400, "Cannot follow this user");
        }
        if (!await db.Follows.AnyAsync(f => f.FollowerId == me.Id && f.FollowingId == other.Id))
        {
            db.Follows.Add(new Follow { FollowerId = me.Id, FollowingId = other.Id });
            await db.SaveChangesAsync();
        }
        return Api.Created("Followed successfully");
    }

    [HttpDelete("{userId:long}/follow")]
    public async Task<IActionResult> Unfollow(long userId)
    {
        var me = await auth.Require(Request, db);
        await Target(userId);
        var follow = await db.Follows.FirstOrDefaultAsync(
            f => f.FollowerId == me.Id && f.FollowingId == userId);
        if (follow is not null)
        {
            db.Follows.Remove(follow);
            await db.SaveChangesAsync();
        }
        return Api.Ok("Unfollowed successfully");
    }

    [HttpGet("{userId:long}/followers")]
    public async Task<IActionResult> Followers(long userId)
    {
        await auth.Require(Request, db);
        await Target(userId);
        var followers = await db.Follows.Include(f => f.Follower)
            .Where(f => f.FollowingId == userId)
            .Select(f => f.Follower)
            .ToListAsync();
        return Api.Ok("Followers", Api.Data("followers", followers));
    }

    [HttpGet("{userId:long}/following")]
    public async Task<IActionResult> Following(long userId)
    {
        await auth.Require(Request, db);
        await Target(userId);
        var following = await db.Follows.Include(f => f.Following)
            .Where(f => f.FollowerId == userId)
            .Select(f => f.Following)
            .ToListAsync();
        return Api.Ok("Following", Api.Data("following", following));
    }

    [HttpGet("{userId:long}/is-following")]
    public async Task<IActionResult> IsFollowing(long userId)
    {
        var me = await auth.Require(Request, db);
        await Target(userId);
        var following = await db.Follows.AnyAsync(
            f => f.FollowerId == me.Id && f.FollowingId == userId);
        return Api.Ok("Follow status", Api.Data("isFollowing", following));
    }
}
