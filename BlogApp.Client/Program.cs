using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlogApp.Client;
using BlogApp.Client.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with API URL
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiUrl = config["ApiUrl"] ?? "http://localhost:5046";
    return new HttpClient { BaseAddress = new Uri(apiUrl) };
});
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Initialize authentication (with error handling)
try
{
    var authService = app.Services.GetRequiredService<AuthService>();
    await authService.InitializeAsync();
}
catch (Exception ex)
{
    // Log error but don't crash the app
    Console.WriteLine($"Auth initialization error: {ex.Message}");
}

// Initialize API service with auth token (with error handling)
try
{
    var apiService = app.Services.GetRequiredService<ApiService>();
    await apiService.InitializeAuthAsync();
}
catch (Exception ex)
{
    // Log error but don't crash the app
    Console.WriteLine($"API initialization error: {ex.Message}");
}

await app.RunAsync();
