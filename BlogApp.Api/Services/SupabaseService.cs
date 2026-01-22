using Supabase;
using Supabase.Gotrue;

namespace BlogApp.Api.Services;

public class SupabaseService : ISupabaseService
{
    private readonly Supabase.Client _supabase;
    private readonly ILogger<SupabaseService> _logger;

    public SupabaseService(IConfiguration configuration, ILogger<SupabaseService> logger)
    {
        var supabaseUrl = configuration["Supabase:Url"] ?? throw new ArgumentNullException("Supabase:Url");
        var supabaseKey = configuration["Supabase:AnonKey"] ?? throw new ArgumentNullException("Supabase:AnonKey");
        
        _supabase = new Supabase.Client(supabaseUrl, supabaseKey);
        _logger = logger;
    }

    public async Task<string?> GetUserIdFromTokenAsync(string token)
    {
        try
        {
            var user = await _supabase.Auth.GetUser(token);
            return user?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user ID from token");
            return null;
        }
    }

    public async Task<User?> GetUserFromTokenAsync(string token)
    {
        try
        {
            var user = await _supabase.Auth.GetUser(token);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user from token");
            return null;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var user = await _supabase.Auth.GetUser(token);
            return user != null;
        }
        catch
        {
            return false;
        }
    }
}
