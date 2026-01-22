using Supabase;
using Blazored.LocalStorage;
using BlogApp.Client.Models;
using BlogApp.Client.Services;

namespace BlogApp.Client.Services;

public class AuthService
{
    private readonly Supabase.Client _supabase;
    private readonly ApiService _apiService;
    private readonly ILocalStorageService _localStorage;
    private UserDto? _currentUser;

    public AuthService(IConfiguration configuration, ApiService apiService, ILocalStorageService localStorage)
    {
        try
        {
            var supabaseUrl = configuration["Supabase:Url"];
            var supabaseKey = configuration["Supabase:AnonKey"];
            
            // Fallback to default values if not in config (for development)
            if (string.IsNullOrEmpty(supabaseUrl))
            {
                supabaseUrl = "https://iubwoldtjrniuzxmvsfr.supabase.co";
            }
            
            if (string.IsNullOrEmpty(supabaseKey))
            {
                supabaseKey = "sb_publishable_UHwAh333vi1hRG7vDX7Ktw_Czq4PXZ2";
            }
            
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                throw new InvalidOperationException("Supabase configuration is missing. Please check appsettings.json");
            }
            
            _supabase = new Supabase.Client(supabaseUrl, supabaseKey);
            _apiService = apiService;
            _localStorage = localStorage;
        }
        catch (Exception ex)
        {
            // Log but don't throw - allow app to continue without auth
            Console.WriteLine($"Failed to initialize Supabase client: {ex.Message}");
            // Set to null so we can check later
            _supabase = null!;
            _apiService = apiService;
            _localStorage = localStorage;
        }
    }

    public UserDto? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;

    public async Task<bool> SignUpAsync(string email, string password, string? username = null)
    {
        try
        {
            if (_supabase == null)
            {
                Console.WriteLine("Supabase client is not initialized");
                throw new Exception("Authentication service is not available. Please refresh the page.");
            }
                
            var response = await _supabase.Auth.SignUp(email, password);

            if (response?.User != null && !string.IsNullOrEmpty(response.User.Id))
            {
                var token = response.AccessToken;
                if (!string.IsNullOrEmpty(token))
                {
                    await _apiService.SetAuthTokenAsync(token);
                    // Use email from Supabase response (it may be normalized)
                    var supabaseEmail = response.User.Email ?? email;
                    await RegisterUserInApiAsync(response.User.Id, username, supabaseEmail);
                    await LoadCurrentUserAsync();
                }
                return true;
            }
            
            Console.WriteLine("SignUp failed: No user in response");
            throw new Exception("Registration failed: No user data received from authentication service.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignUp error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            
            // Check for specific Supabase error messages
            var errorMessage = ex.Message.ToLower();
            
            // Email already exists
            if (errorMessage.Contains("user already registered") || 
                errorMessage.Contains("already registered") ||
                errorMessage.Contains("email already") ||
                errorMessage.Contains("already exists"))
            {
                throw new Exception("This email address is already registered. Please use a different email or try logging in.");
            }
            
            // Password requirements
            if (errorMessage.Contains("password") && (errorMessage.Contains("weak") || errorMessage.Contains("short") || errorMessage.Contains("minimum")))
            {
                throw new Exception("Password is too weak. Please use a stronger password (at least 6 characters).");
            }
            
            // Invalid email format
            if (errorMessage.Contains("invalid email") || errorMessage.Contains("email format"))
            {
                throw new Exception("Invalid email address format. Please check your email and try again.");
            }
            
            // 400 Bad Request - generic Supabase error
            if (errorMessage.Contains("400") || errorMessage.Contains("bad request"))
            {
                // Try to extract more specific error from the exception
                var detailedError = ex.Message;
                if (ex.InnerException != null)
                {
                    detailedError = ex.InnerException.Message;
                }
                
                // Check for common 400 errors
                if (detailedError.Contains("User already registered"))
                {
                    throw new Exception("This email is already registered. Please try logging in instead.");
                }
                
                throw new Exception($"Registration failed: {detailedError}. Please check your information and try again.");
            }
            
            // Re-throw with original message if we can't categorize it
            throw new Exception($"Registration failed: {ex.Message}");
        }
    }

    public async Task<bool> SignInAsync(string email, string password)
    {
        try
        {
            if (_supabase == null)
            {
                Console.WriteLine("Supabase client is not initialized");
                return false;
            }
                
            var response = await _supabase.Auth.SignIn(email, password);
            
            if (response != null && !string.IsNullOrEmpty(response.AccessToken))
            {
                var token = response.AccessToken;
                await _apiService.SetAuthTokenAsync(token);
                if (response.User != null && !string.IsNullOrEmpty(response.User.Id))
                {
                    // Use email from Supabase response (it may be normalized)
                    var supabaseEmail = response.User.Email ?? email;
                    // Register user in our database if they don't exist
                    await RegisterUserInApiAsync(response.User.Id, null, supabaseEmail);
                    // Wait a bit for the registration to complete
                    await Task.Delay(500);
                }
                // Load current user - this will create the user if registration succeeded
                await LoadCurrentUserAsync();
                
                // If user still not found, try registering again
                if (_currentUser == null && response?.User != null && !string.IsNullOrEmpty(response.User.Id))
                {
                    Console.WriteLine("User not found in database, attempting to register...");
                    var supabaseEmail = response.User.Email ?? email;
                    await RegisterUserInApiAsync(response.User.Id, null, supabaseEmail);
                    await Task.Delay(500);
                    await LoadCurrentUserAsync();
                }
                
                return _currentUser != null;
            }
            
            Console.WriteLine("SignIn failed: No access token in response");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignIn error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            // Check for specific Supabase error messages
            var errorMessage = ex.Message.ToLower();
            if (errorMessage.Contains("email not confirmed") || errorMessage.Contains("email_not_confirmed") || errorMessage.Contains("email_not_verified"))
            {
                throw new Exception("Please verify your email address before logging in. Check your inbox for the verification email.");
            }
            if (errorMessage.Contains("invalid") || errorMessage.Contains("credentials") || errorMessage.Contains("400"))
            {
                throw new Exception("Invalid email or password. Please check your credentials.");
            }
            throw new Exception($"Login failed: {ex.Message}");
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            if (_supabase != null)
                await _supabase.Auth.SignOut();
            await _apiService.SetAuthTokenAsync(null);
            _currentUser = null;
        }
        catch { }
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            // Check if Supabase client is properly initialized
            if (_supabase == null || _supabase.Auth == null)
                return false;
                
            var session = _supabase.Auth.CurrentSession;
            if (session != null && !string.IsNullOrEmpty(session.AccessToken))
            {
                await _apiService.SetAuthTokenAsync(session.AccessToken);
                await LoadCurrentUserAsync();
                return true;
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - allow app to continue
            Console.WriteLine($"Auth initialization error: {ex.Message}");
        }
        
        return false;
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        try
        {
            if (_supabase == null)
                return false;
                
            await _supabase.Auth.ResetPasswordForEmail(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task RegisterUserInApiAsync(string supabaseUserId, string? username, string? email)
    {
        try
        {
            var result = await _apiService.PostAsync<UserDto>("api/auth/register", new
            {
                SupabaseUserId = supabaseUserId,
                Username = username,
                Email = email
            });
            
            if (result != null)
            {
                Console.WriteLine($"User registered in API: {result.Email}");
            }
            else
            {
                Console.WriteLine("Failed to register user in API");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering user in API: {ex.Message}");
            // Don't throw - this is not critical for registration
        }
    }

    private async Task LoadCurrentUserAsync()
    {
        try
        {
            _currentUser = await _apiService.GetAsync<UserDto>("api/auth/me");
            if (_currentUser == null)
            {
                // User not logged in or session expired - this is normal
                Console.WriteLine("No current user found in database (user may need to be registered)");
            }
            else
            {
                Console.WriteLine($"Current user loaded: {_currentUser.Email}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading current user: {ex.Message}");
            _currentUser = null;
        }
    }
}
