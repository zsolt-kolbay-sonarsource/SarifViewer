using ReactiveUI;

namespace SarifViewer.Models;

public class ApplicationSettings : ReactiveObject
{
    private string repositoryFolderPath = "";
    public string RepositoryFolderPath
    {
        get => repositoryFolderPath;
        set => this.RaiseAndSetIfChanged(ref repositoryFolderPath, value);
    }

    private FilterSettings filter = new();
    public FilterSettings Filter
    {
        get => filter;
        set => this.RaiseAndSetIfChanged(ref filter, value);
    }
}
