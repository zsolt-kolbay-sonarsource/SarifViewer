using SarifViewer.Models.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SarifViewer.Models;

public class ApplicationSettingsFileService
{
    public const string ExpectedIssuesFolder = @"analyzers\its\expected";
    public const string ActualIssuesFolder = @"analyzers\its\actual";
    public const string SourceCodeFolder = @"analyzers\its\sources";
    public const string IntegrationTestFolder = @"analyzers\its";

    public static async Task<string> ReadSourceCodeFromFile(string repositoryPath, string relativeSourcePath)
    {
        string fullPath = Path.Combine(repositoryPath, IntegrationTestFolder, relativeSourcePath);
        return await File.ReadAllTextAsync(fullPath);
    }

    public static void ValidateRepositoryFolder(string folderPath)
    {
        var requiredRepoSubFolders = new[]
        {
            ExpectedIssuesFolder,
            SourceCodeFolder
        };

        if (!Directory.Exists(folderPath))
        {
            throw new ValidationException($"Sonar-Dotnet repository folder '{folderPath}' does not exist or is inaccessible.");
        }

        var firstMissingSubFolder = requiredRepoSubFolders.FirstOrDefault(subFolder => !Directory.Exists(Path.Combine(folderPath, subFolder)));
        if (firstMissingSubFolder != null)
        {
            throw new ValidationException($"Unable to find subfolder '{firstMissingSubFolder}' under '{folderPath}'");
        }
    }

    public static async Task<bool> SaveToFileAsync(string filePath, ApplicationSettings settings)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            string jsonString = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(filePath, jsonString);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<ApplicationSettings> LoadFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new ApplicationSettings();
            }

            string jsonString = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var settings = JsonSerializer.Deserialize<ApplicationSettings>(jsonString, options);
            return settings;
        }
        catch (Exception)
        {
            return new ApplicationSettings();
        }
    }
}
