namespace BlogApp.Client.Models;

public class UserDto
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public string? Twitter { get; set; }
    public string? LinkedIn { get; set; }
    public string? GitHub { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
