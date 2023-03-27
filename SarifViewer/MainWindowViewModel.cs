using ReactiveUI;
using SarifViewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SarifViewer;

public class MainWindowViewModel : ReactiveObject
{
    private const string applicationSettingsFilePath = "settings.json";

    private string sourceCode = "";
    public string SourceCode
    {
        get => sourceCode;
        set => this.RaiseAndSetIfChanged(ref sourceCode, value);
    }

    private bool isLoading;
    public bool IsLoading
    {
        get => isLoading;
        set => this.RaiseAndSetIfChanged(ref isLoading, value);
    }

    private ApplicationSettings settings = new();
    public ApplicationSettings Settings
    {
        get => settings;
        set => this.RaiseAndSetIfChanged(ref settings, value);
    }

    public ObservableCollection<Issue> ExpectedIssues { get; private set; } = new();
    public ObservableCollection<Issue> ActualIssues { get; private set; } = new();
    public ObservableCollection<Issue> LostIssues { get; private set; } = new();
    public ObservableCollection<Issue> NewIssues { get; private set; } = new();

    public Action<Location> ScrollToLocation { get; set; }

    public ICommand ApplicationLoadedCommand => ReactiveCommand.CreateFromTask(async () =>
    {
        Settings = await ApplicationSettingsFileService.LoadFromFileAsync(applicationSettingsFilePath);
        if (!string.IsNullOrWhiteSpace(Settings.RepositoryFolderPath))
        {
            await LoadAllIssues(Settings.RepositoryFolderPath);
        }
    });

    public ICommand ApplicationClosingCommand => ReactiveCommand.CreateFromTask(async () =>
        await ApplicationSettingsFileService.SaveToFileAsync(applicationSettingsFilePath, Settings));

    public ICommand SelectRepositoryFolderCommand => ReactiveCommand.CreateFromTask(async () =>
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        var result = dialog.ShowDialog();

        if (result == System.Windows.Forms.DialogResult.OK)
        {
            await LoadAllIssues(dialog.SelectedPath);
        }
    });

    public ICommand SelectedIssueCommand => ReactiveCommand.CreateFromTask<Issue>(async issue =>
    {
        if (issue != null)
        {
            try
            {
                SourceCode = await ApplicationSettingsFileService.ReadSourceCodeFromFile(Settings.RepositoryFolderPath, issue.FirstLocationUri);
                if (issue.Location.Any())
                {
                    ScrollToLocation(issue.Location.First());
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load source file '{issue.FirstLocationUri}'" + Environment.NewLine + ex.Message);
            }
        }
    });

    private readonly ObservableAsPropertyHelper<IEnumerable<Issue>> filteredIssues;
    public IEnumerable<Issue> FilteredIssues => filteredIssues.Value;

    public MainWindowViewModel()
    {
        filteredIssues = this
            .WhenAnyValue(x => x.Settings.Filter, x => x.ExpectedIssues)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Select(x => FilterIssues(ExpectedIssues))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.FilteredIssues);
    }

    private IEnumerable<Issue> FilterIssues(IEnumerable<Issue> issues) =>
        issues.Where(x => x.Id.Contains(Settings.Filter.IssueId, StringComparison.InvariantCultureIgnoreCase));

    private async Task LoadAllIssues(string repositoryPath)
    {
        try
        {
            IsLoading = true;
            await Task.Run(() => ApplicationSettingsFileService.ValidateRepositoryFolder(repositoryPath));
            Settings.RepositoryFolderPath = repositoryPath;
            await LoadIssues();
            IsLoading = false;
        }
        catch (Exception ex)
        {
            IsLoading = false;
            ShowError("Failed to load JSON files." + Environment.NewLine + ex.Message);
        }
    }

    private async Task LoadIssues()
    {
        var expectedIssuesPath = Path.Combine(Settings.RepositoryFolderPath, ApplicationSettingsFileService.ExpectedIssuesFolder);
        var expectedIssues = await IssueReader.ReadFromFolder(expectedIssuesPath);
        ExpectedIssues = new(expectedIssues.Where(x => x.Id.Contains(Settings.Filter.IssueId)).Distinct());
        var actualIssuesPath = Path.Combine(Settings.RepositoryFolderPath, ApplicationSettingsFileService.ActualIssuesFolder);
        var actualIssues = await IssueReader.ReadFromFolder(actualIssuesPath);
        ActualIssues = new(actualIssues.Where(x => x.Id.Contains(Settings.Filter.IssueId)).Distinct());

        LostIssues = new(ExpectedIssues.Except(ActualIssues));
        NewIssues = new(ActualIssues.Except(ExpectedIssues));
        this.RaisePropertyChanged(nameof(LostIssues));
        this.RaisePropertyChanged(nameof(NewIssues));
    }

    private static void ShowError(string message) =>
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

}
