using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SarifViewer.Models;

public class GitHubFolderDownloader
{
    private const string GitHubApiBaseUrl = "https://api.github.com/repos/";
    private const string ArchiveFormat = "zipball";
    private readonly HttpClient httpClient;

    public GitHubFolderDownloader()
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
    }

    public async Task DownloadFolderAsync(string repoName, string folderPath, string localDestination, string branchName = "master", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repoName) || string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(localDestination))
        {
            throw new ArgumentException("All input parameters must be non-empty strings.");
        }

        string commitId = await GetCommitIdAsync(repoName, branchName, cancellationToken);
        string sanitizedBranchName = SanitizeWindowsFilePath(branchName);
        string destinationPath = Path.Combine(localDestination, sanitizedBranchName, commitId);

        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);

            string zipFilePath = await DownloadRepoAsync(repoName, branchName, destinationPath, cancellationToken);

            try
            {
                await ExtractFolderAsync(zipFilePath, folderPath, destinationPath, cancellationToken);
            }
            finally
            {
                File.Delete(zipFilePath);
            }
        }
    }

    public async Task<string[]> ListBranchNamesAsync(string repoName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repoName))
        {
            throw new ArgumentException("Repository name must be a non-empty string.");
        }

        string apiUrl = $"{GitHubApiBaseUrl}{repoName}/branches";
        HttpResponseMessage response = await httpClient.GetAsync(apiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        List<BranchItem> branches = JsonSerializer.Deserialize<List<BranchItem>>(content);
        string[] branchNames = branches.Select(b => b.name).ToArray();

        Array.Sort(branchNames, (x, y) => {
            if (x == "master" || x == "main")
            {
                return -1;
            }
            if (y == "master" || y == "main")
            {
                return 1;
            }
            return x.CompareTo(y);
        });

        return branchNames;
    }

    private async Task<string> DownloadRepoAsync(string repoName, string branchName, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(repoName) || string.IsNullOrEmpty(destinationPath))
        {
            throw new ArgumentException("Repo name and destination path parameters must be non-empty strings.");
        }

        string downloadUrl = $"{GitHubApiBaseUrl}{repoName}/{ArchiveFormat}/{branchName}";
        string zipFilePath = Path.Combine(destinationPath, $"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.zip");

        using (FileStream fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            await response.Content.CopyToAsync(fileStream, cancellationToken);
        }

        return zipFilePath;
    }

    private async Task ExtractFolderAsync(string zipFilePath, string folderPath, string localDestination, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(zipFilePath) || string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(localDestination))
        {
            throw new ArgumentException("All input parameters must be non-empty strings.");
        }

        using var fileStream = File.OpenRead(zipFilePath);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
        string folderPrefix = archive.Entries[0].FullName.Split('/')[0];
        string searchFolderPath = $"{folderPrefix}/{folderPath}";

        int foundEntries = 0;

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (entry.FullName.StartsWith(searchFolderPath, StringComparison.OrdinalIgnoreCase) && !entry.FullName.Equals(searchFolderPath, StringComparison.OrdinalIgnoreCase))
            {
                foundEntries++;

                string destinationPath = Path.Combine(localDestination, entry.FullName.Substring(folderPrefix.Length + 1));

                if (entry.FullName.EndsWith("/"))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    using (Stream entryStream = entry.Open())
                    using (Stream destinationStream = File.OpenWrite(destinationPath))
                    {
                        await entryStream.CopyToAsync(destinationStream, cancellationToken);
                    }
                }
            }
        }

        if (foundEntries == 0)
        {
            throw new InvalidOperationException($"The folder '{folderPath}' was not found in the repository.");
        }
    }

    private static string SanitizeWindowsFilePath(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input must be a non-empty string.");
        }

        char[] invalidChars = new char[] { '/', ':', '*', '?', '"', '<', '>', '|' };
        var sanitizedString = new StringBuilder(input.Length);

        foreach (char ch in input)
        {
            sanitizedString.Append(invalidChars.Contains(ch) ? '-' : ch);
        }

        return sanitizedString.ToString();
    }

    private async Task<string> GetCommitIdAsync(string repoName, string branchName, CancellationToken cancellationToken = default)
    {
        string apiUrl = $"{GitHubApiBaseUrl}{repoName}/branches/{branchName}";
        HttpResponseMessage response = await httpClient.GetAsync(apiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        BranchResponse responseObject = JsonSerializer.Deserialize<BranchResponse>(content);
        return responseObject.commit.sha;
    }


    private class BranchResponse
    {
        public CommitObject commit { get; set; }

        public class CommitObject
        {
            public string sha { get; set; }
        }
    }

    private class BranchItem
    {
        public string name { get; set; }
    }
}

