using ReactiveUI;

namespace SarifViewer.Models;

public class FilterSettings : ReactiveObject
{
    private string issueId = "";
    public string IssueId
    {
        get => issueId;
        set => this.RaiseAndSetIfChanged(ref issueId, value);
    }

    private string sourceFilePath = "";
    public string SourceFilePath
    {
        get => sourceFilePath;
        set => this.RaiseAndSetIfChanged(ref sourceFilePath, value);
    }

    private string issueMessage = "";
    public string IssueMessage
    {
        get => issueMessage;
        set => this.RaiseAndSetIfChanged(ref issueMessage, value);
    }

    private IssueState issueState;
    public IssueState IssueState
    {
        get => issueState;
        set => this.RaiseAndSetIfChanged(ref issueState, value);
    }

    private IssueLanguage issueLanguage;
    public IssueLanguage IssueLanguage
    {
        get => issueLanguage;
        set => this.RaiseAndSetIfChanged(ref issueLanguage, value);
    }
}
