using Supabase.Gotrue;

namespace BlogApp.Api.Services;

public interface ISupabaseService
{
    Task<string?> GetUserIdFromTokenAsync(string token);
    Task<User?> GetUserFromTokenAsync(string token);
    Task<bool> ValidateTokenAsync(string token);
}
