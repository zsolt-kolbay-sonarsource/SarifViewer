using System.Collections.Generic;
using System.IO;
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
        var issues = JsonSerializer.Deserialize<IssueData>(sanatizedContent, options).Issues;

        string relativePath = Path.GetRelativePath(rootFolderPath, filePath);
        issues.ForEach(issue => issue.JsonFilePath = relativePath);

        return issues;
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

    private record IssueData(List<Issue> Issues);
}