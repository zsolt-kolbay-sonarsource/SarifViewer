﻿using ReactiveUI;
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

    private static readonly ValueWithDisplayName<IssueState>[] issueTypeOptionsOnlyExpected = new[]
    {
        new ValueWithDisplayName<IssueState> { DisplayName = "Expected", Value = IssueState.Expected }
    };

    private static readonly ValueWithDisplayName<IssueState>[] issueTypeOptionsActualAndExpected = new[]
    {
        new ValueWithDisplayName<IssueState> { DisplayName = "Expected", Value = IssueState.Expected },
        new ValueWithDisplayName<IssueState> { DisplayName = "Actual", Value = IssueState.Actual },
        new ValueWithDisplayName<IssueState> { DisplayName = "Lost", Value = IssueState.Lost },
        new ValueWithDisplayName<IssueState> { DisplayName = "New", Value = IssueState.New }
    };

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

    private ObservableCollection<Issue> expectedIssues = new();
    public ObservableCollection<Issue> ExpectedIssues
    {
        get => expectedIssues;
        private set => this.RaiseAndSetIfChanged(ref expectedIssues, value);
    }

    private ObservableCollection<Issue> actualIssues = new();
    public ObservableCollection<Issue> ActualIssues
    {
        get => actualIssues;
        private set => this.RaiseAndSetIfChanged(ref actualIssues, value);
    }

    private ObservableCollection<Issue> lostIssues = new();
    public ObservableCollection<Issue> LostIssues
    {
        get => lostIssues;
        private set => this.RaiseAndSetIfChanged(ref lostIssues, value);
    }

    private ObservableCollection<Issue> newIssues = new();
    public ObservableCollection<Issue> NewIssues
    {
        get => newIssues;
        private set => this.RaiseAndSetIfChanged(ref newIssues, value);
    }

    private ObservableCollection<ValueWithDisplayName<IssueState>> issueTypeOptions = new(issueTypeOptionsActualAndExpected);
    public ObservableCollection<ValueWithDisplayName<IssueState>> IssueTypeOptions
    {
        get => issueTypeOptions;
        private set => this.RaiseAndSetIfChanged(ref issueTypeOptions, value);
    }

    public ValueWithDisplayName<IssueLanguage>[] IssueLanguageOptions => new[]
    {
        new ValueWithDisplayName<IssueLanguage> { DisplayName = "C# and Visual Basic", Value = IssueLanguage.CSharpAndVisualBasic },
        new ValueWithDisplayName<IssueLanguage> { DisplayName = "Only C#", Value = IssueLanguage.CSharp },
        new ValueWithDisplayName<IssueLanguage> { DisplayName = "Only Visual Basic", Value = IssueLanguage.VisualBasic }
    };

    public Action<Location> ScrollToLocation { get; set; }

    public ICommand ApplicationLoadedCommand => ReactiveCommand.CreateFromTask(async () =>
    {
        Settings = await ApplicationFileService.LoadFromFileAsync(applicationSettingsFilePath);
        if (!string.IsNullOrWhiteSpace(Settings.RepositoryFolderPath))
        {
            await LoadAllIssues(Settings.RepositoryFolderPath);
        }
    });

    public ICommand ApplicationClosingCommand => ReactiveCommand.CreateFromTask(async () =>
        await ApplicationFileService.SaveToFileAsync(applicationSettingsFilePath, Settings));

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
        if (issue != null && issue.Location?.FirstOrDefault() is { Uri: not null } firstLocation)
        {
            try
            {
                SourceCode = await ApplicationFileService.ReadSourceCodeFromFile(Settings.RepositoryFolderPath, firstLocation.Uri);
                ScrollToLocation(firstLocation);
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
            .WhenAnyValue(
                x => x.Settings.Filter.IssueId,
                x => x.Settings.Filter.SourceFilePath,
                x => x.Settings.Filter.IssueMessage,
                x => x.Settings.Filter.IssueLanguage,
                x => x.Settings.Filter.IssueState,
                x => x.ExpectedIssues,
                x => x.ActualIssues)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .DistinctUntilChanged()
            .Select(x => FilterIssues(SelectIssueList()))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.FilteredIssues);
    }

    private ObservableCollection<Issue> SelectIssueList() =>
        Settings.Filter.IssueState switch
        {
            IssueState.Expected => ExpectedIssues,
            IssueState.Actual => ActualIssues,
            IssueState.Lost => LostIssues,
            IssueState.New => NewIssues,
            _ => new()
        };

    private IEnumerable<Issue> FilterIssues(IEnumerable<Issue> issues) =>
        issues.Where(x =>
            (string.IsNullOrWhiteSpace(Settings.Filter.IssueId) || x.Id.Contains(Settings.Filter.IssueId, StringComparison.InvariantCultureIgnoreCase))
         && (string.IsNullOrWhiteSpace(Settings.Filter.SourceFilePath) || (x.Location != null && x.Location.Any(loc => loc != null && loc.Uri.Contains(Settings.Filter.SourceFilePath, StringComparison.InvariantCultureIgnoreCase))))
         && (string.IsNullOrWhiteSpace(Settings.Filter.IssueMessage) || x.Message.Contains(Settings.Filter.IssueMessage, StringComparison.InvariantCultureIgnoreCase))
         && IsIssueUsingSelectedLanguage(x))
        .OrderBy(x => x.Location?.FirstOrDefault()?.Uri);

    private bool IsIssueUsingSelectedLanguage(Issue issue) =>
         Settings.Filter.IssueLanguage == IssueLanguage.CSharpAndVisualBasic
         || issue.Location == null
         || (Settings.Filter.IssueLanguage == IssueLanguage.CSharp && issue.FirstLocationUri.EndsWith(".cs"))
         || (Settings.Filter.IssueLanguage == IssueLanguage.VisualBasic && issue.FirstLocationUri.EndsWith(".vb"));

    private async Task LoadAllIssues(string repositoryPath)
    {
        try
        {
            IsLoading = true;
            await Task.Run(() => ApplicationFileService.ValidateRepositoryFolder(repositoryPath));
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
        var expectedIssuesPath = Path.Combine(Settings.RepositoryFolderPath, ApplicationFileService.ExpectedIssuesFolder);
        var loadedExpectedIssues = await IssueReader.ReadFromFolder(expectedIssuesPath);
        ExpectedIssues = new(loadedExpectedIssues.Distinct());

        if (ApplicationFileService.ActualTestResultFolderExists(Settings.RepositoryFolderPath))
        {
            var actualIssuesPath = Path.Combine(Settings.RepositoryFolderPath, ApplicationFileService.ActualIssuesFolder);
            var loadedActualIssues = await IssueReader.ReadFromFolder(actualIssuesPath);
            ActualIssues = new(loadedActualIssues.Distinct());
            LostIssues = new(ExpectedIssues.Except(ActualIssues));
            NewIssues = new(ActualIssues.Except(ExpectedIssues));

            if (!Enumerable.SequenceEqual(IssueTypeOptions, issueTypeOptionsActualAndExpected))
            {
                IssueTypeOptions.AddRange(IssueTypeOptions.Except(issueTypeOptionsActualAndExpected));
            }
        }
        else
        {
            ActualIssues = new();
            LostIssues = new();
            NewIssues = new();

            Settings.Filter.IssueState = IssueState.Expected;

            if (!Enumerable.SequenceEqual(IssueTypeOptions, issueTypeOptionsActualAndExpected))
            {
                IssueTypeOptions.Clear();
                IssueTypeOptions.AddRange(issueTypeOptionsOnlyExpected);
            }
        }
    }

    private static void ShowError(string message) =>
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

}