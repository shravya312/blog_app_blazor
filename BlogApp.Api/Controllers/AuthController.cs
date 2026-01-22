using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Api.Data;
using BlogApp.Api.DTOs;
using BlogApp.Api.Models;
using BlogApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using SupabaseUser = Supabase.Gotrue.User;

namespace BlogApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISupabaseService _supabaseService;

    public AuthController(ApplicationDbContext context, ISupabaseService supabaseService)
    {
        _context = context;
        _supabaseService = supabaseService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] CreateUserDto dto)
    {
        var supabaseUserId = await _supabaseService.GetUserIdFromTokenAsync(
            Request.Headers["Authorization"].ToString().Replace("Bearer ", ""));

        if (string.IsNullOrEmpty(supabaseUserId))
            return Unauthorized("Invalid token");

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);

        if (existingUser != null)
        {
            // Update email if it's different (in case Supabase normalized it)
            if (!string.IsNullOrEmpty(dto.Email) && existingUser.Email != dto.Email)
            {
                existingUser.Email = dto.Email;
                existingUser.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Ok(MapToDto(existingUser));
        }

        var user = new User
        {
            SupabaseUserId = supabaseUserId,
            Username = dto.Username,
            Email = dto.Email?.ToLowerInvariant().Trim() ?? string.Empty, // Normalize email
            Role = UserRole.Reader
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(MapToDto(user));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var supabaseUserId = await _supabaseService.GetUserIdFromTokenAsync(
            Request.Headers["Authorization"].ToString().Replace("Bearer ", ""));

        if (string.IsNullOrEmpty(supabaseUserId))
            return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);

        if (user == null)
        {
            // User exists in Supabase but not in our database - auto-create them
            // Get user info from Supabase
            try
            {
                var supabaseUser = await _supabaseService.GetUserFromTokenAsync(
                    Request.Headers["Authorization"].ToString().Replace("Bearer ", ""));
                
                if (supabaseUser != null)
                {
                    user = new Models.User
                    {
                        SupabaseUserId = supabaseUserId,
                        Email = supabaseUser.Email?.ToLowerInvariant().Trim() ?? string.Empty,
                        Username = supabaseUser.UserMetadata?.ContainsKey("username") == true 
                            ? supabaseUser.UserMetadata["username"]?.ToString() 
                            : null,
                        Role = UserRole.Reader
                    };
                    
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                return NotFound("User not found in database");
            }
        }

        if (user == null)
            return NotFound();

        return Ok(MapToDto(user));
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
