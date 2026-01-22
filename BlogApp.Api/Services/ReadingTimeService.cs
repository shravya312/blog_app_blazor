using Markdig;

namespace BlogApp.Api.Services;

public class ReadingTimeService
{
    private const int WordsPerMinute = 200;

    public int CalculateReadingTime(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        // Convert markdown to plain text
        var pipeline = new MarkdownPipelineBuilder().Build();
        var plainText = Markdown.ToPlainText(content, pipeline);

        // Count words
        var words = plainText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;

        // Calculate reading time
        var readingTime = (int)Math.Ceiling((double)wordCount / WordsPerMinute);
        return Math.Max(1, readingTime); // At least 1 minute
    }
}
