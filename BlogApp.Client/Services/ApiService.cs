using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace BlogApp.Client.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly IConfiguration _configuration;

    public ApiService(HttpClient httpClient, ILocalStorageService localStorage, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _configuration = configuration;
        
        // Set BaseAddress - override the default one
        var apiUrl = configuration["ApiUrl"] ?? "http://localhost:5046";
        _httpClient.BaseAddress = new Uri(apiUrl);
        
        Console.WriteLine($"ApiService initialized with BaseAddress: {_httpClient.BaseAddress}");
    }

    public async Task SetAuthTokenAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            await _localStorage.RemoveItemAsync("authToken");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await _localStorage.SetItemAsync("authToken", token);
        }
    }

    public async Task InitializeAuthAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                // Don't log 401 (Unauthorized) as an error - it's expected when not logged in
                if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error ({response.StatusCode}): {content}");
                }
                return default;
            }
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            // Check if response is HTML (error page) instead of JSON
            if (jsonContent.TrimStart().StartsWith("<"))
            {
                Console.WriteLine($"API returned HTML instead of JSON: {jsonContent.Substring(0, Math.Min(200, jsonContent.Length))}");
                return default;
            }
            
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (HttpRequestException ex)
        {
            // Don't log 401 errors - they're expected
            if (!ex.Message.Contains("401"))
            {
                Console.WriteLine($"HTTP Request Error: {ex.Message}");
            }
            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetAsync: {ex.Message}");
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (HttpRequestException)
        {
            return default;
        }
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (HttpRequestException)
        {
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
