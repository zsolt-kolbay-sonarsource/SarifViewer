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

    private string filePath = "";
    public string FilePath
    {
        get => filePath;
        set => this.RaiseAndSetIfChanged(ref filePath, value);
    }

    private string issueMessage = "";
    public string IssueMessage
    {
        get => issueMessage;
        set => this.RaiseAndSetIfChanged(ref issueMessage, value);
    }

    private IssueType issueType;
    public IssueType IssueType
    {
        get => issueType;
        set => this.RaiseAndSetIfChanged(ref issueType, value);
    }
}
