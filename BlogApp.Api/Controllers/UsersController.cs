using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BlogApp.Api.Data;
using BlogApp.Api.DTOs;
using BlogApp.Api.Models;
using BlogApp.Api.Services;

namespace BlogApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISupabaseService _supabaseService;

    public UsersController(ApplicationDbContext context, ISupabaseService supabaseService)
    {
        _context = context;
        _supabaseService = supabaseService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        return Ok(MapToDto(user));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateUserProfileDto dto)
    {
        var supabaseUserId = await _supabaseService.GetUserIdFromTokenAsync(
            Request.Headers["Authorization"].ToString().Replace("Bearer ", ""));

        if (string.IsNullOrEmpty(supabaseUserId))
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);

        if (user == null)
            return NotFound();

        user.Username = dto.Username ?? user.Username;
        user.AvatarUrl = dto.AvatarUrl ?? user.AvatarUrl;
        user.Bio = dto.Bio ?? user.Bio;
        user.Website = dto.Website ?? user.Website;
        user.Twitter = dto.Twitter ?? user.Twitter;
        user.LinkedIn = dto.LinkedIn ?? user.LinkedIn;
        user.GitHub = dto.GitHub ?? user.GitHub;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(MapToDto(user));
    }

    [HttpPut("{id}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateUserRole(int id, [FromBody] string role)
    {
        if (!Enum.TryParse<UserRole>(role, out var userRole))
            return BadRequest("Invalid role");

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        user.Role = userRole;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users.Select(MapToDto));
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Website = user.Website,
            Twitter = user.Twitter,
            LinkedIn = user.LinkedIn,
            GitHub = user.GitHub,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };
    }
}
