﻿using SarifViewer.Models.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SarifViewer.Models;

public static class ApplicationFileService
{
    public const string ExpectedIssuesFolder = @"analyzers\its\expected";
    public const string ActualIssuesFolder = @"analyzers\its\actual";
    public const string SourceCodeFolder = @"analyzers\its\sources";
    public const string IntegrationTestFolder = @"analyzers\its";

    public static bool ActualTestResultFolderExists(string repositoryPath) =>
        Directory.Exists(Path.Combine(repositoryPath, ActualIssuesFolder));

    public static string SourceCodePath(string repositoryPath, string relativeSourcePath)
        => Path.Combine(repositoryPath, IntegrationTestFolder, relativeSourcePath);

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
                Converters =
                {
                    new SkipBaseClassPropertiesJsonConverter<ApplicationSettings>(),
                    new SkipBaseClassPropertiesJsonConverter<FilterSettings>()
                }
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
                PropertyNameCaseInsensitive = true
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
