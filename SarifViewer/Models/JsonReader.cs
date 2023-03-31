using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SarifViewer.Models;

public static class IssueReader
{
    private static readonly Regex messageNodeRegex = new(
        @"""message""\s*:\s*""(.*?)""\s*,\s*""location""\s*:",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline
    );

    public static string FixUnescapedQuotesInMessageNode(string input) =>
        messageNodeRegex.Replace(input, match =>
        {
            string originalValue = match.Groups[1].Value;
            string fixedValue = originalValue
                .Replace("\"", "\\\"")
                .Replace("\n", "")
                .Replace("\r", "");
            return $@"""message"": ""{fixedValue}"", ""location"":";
        });

    public static async Task<List<Issue>> ReadFromFile(string filePath, string rootFolderPath = null)
    {
        string jsonData = await File.ReadAllTextAsync(filePath);

        string sanatizedContent = FixUnescapedQuotesInMessageNode(jsonData.Replace("\\", "\\\\"));

        var options = new JsonSerializerOptions { Converters = { new IssueJsonConverter() }, PropertyNameCaseInsensitive = true };
        var loadedIssues = JsonSerializer.Deserialize<IssueData>(sanatizedContent, options).Issues;
        string relativePath = Path.GetRelativePath(rootFolderPath, filePath);
        var issues = RemoveTempPathsAndAddJsonPath(loadedIssues, relativePath);

        return issues;
    }

    private static List<Issue> RemoveTempPathsAndAddJsonPath(List<Issue> issues, string jsonFilePath)
    {
        issues.ForEach(issue => issue.JsonFilePath = jsonFilePath);
        return issues
            .Where(issue =>
                issue.Location == null ||
                !issue.Location.Any(loc => loc != null
                                           && loc.Uri.StartsWith("Replaced-Temporary-Path\\")))
            .ToList();
    }

    public static async Task<List<Issue>> ReadFromFolder(string folderPath)
    {
        var allIssues = new List<Issue>();
        var jsonFiles = Directory.GetFiles(folderPath, "*.json", SearchOption.AllDirectories);

        foreach (var jsonFile in jsonFiles)
        {
            var issuesInSingleFile = await ReadFromFile(jsonFile, folderPath);
            allIssues.AddRange(issuesInSingleFile);
        }

        return allIssues;
    }

    private sealed record IssueData(List<Issue> Issues);
}