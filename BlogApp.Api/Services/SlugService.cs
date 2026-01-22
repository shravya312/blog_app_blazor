using System.Text.RegularExpressions;

namespace BlogApp.Api.Services;

public class SlugService
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convert to lowercase
        text = text.ToLowerInvariant();

        // Remove accents
        text = RemoveAccents(text);

        // Replace spaces with hyphens
        text = Regex.Replace(text, @"\s+", "-");

        // Remove invalid characters
        text = Regex.Replace(text, @"[^a-z0-9\-]", "");

        // Replace multiple hyphens with single hyphen
        text = Regex.Replace(text, @"-+", "-");

        // Trim hyphens from start and end
        text = text.Trim('-');

        return text;
    }

    private static string RemoveAccents(string text)
    {
        byte[] bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(text);
        return System.Text.Encoding.ASCII.GetString(bytes);
    }
}
