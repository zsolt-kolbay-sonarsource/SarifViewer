using SarifViewer.Models;

namespace SarifViewer;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        ((MainWindowViewModel)DataContext).ScrollToLocation = ScrollToLocationInSourceCode;
    }

    private void ScrollToLocationInSourceCode(Location location)
    {
        int startPosition = SourceCodeTextBox.GetCharacterIndexFromLineIndex(location.Region.StartLine - 1) + location.Region.StartColumn - 1;
        int endPosition = SourceCodeTextBox.GetCharacterIndexFromLineIndex(location.Region.EndLine - 1) + location.Region.EndColumn - 1;
        SourceCodeTextBox.Select(startPosition, endPosition - startPosition);
        SourceCodeTextBox.ScrollToLine(location.Region.StartLine - 1);
        SourceCodeTextBox.Focus();
    }
}
