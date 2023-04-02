using System;
using System.IO;
using System.Linq;
using System.Windows;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using SarifViewer.Models;

namespace SarifViewer.Views;

public class SourceCodeEditor : TextEditor
{
    public static readonly DependencyProperty SourceCodePathProperty = DependencyProperty.Register(
        "SourceCodePath", typeof(string), typeof(SourceCodeEditor), new PropertyMetadata(null, OnSourceCodePathChanged));

    public static readonly DependencyProperty IssuesInFileProperty = DependencyProperty.Register(
        "IssuesInFile", typeof(Issue[]), typeof(SourceCodeEditor), new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedIssueProperty = DependencyProperty.Register(
        "SelectedIssue", typeof(Issue), typeof(SourceCodeEditor), new PropertyMetadata(null, OnSelectedIssueChanged));

    public string SourceCodePath
    {
        get => (string)GetValue(SourceCodePathProperty);
        set => SetValue(SourceCodePathProperty, value);
    }

    public Issue[] IssuesInFile
    {
        get => (Issue[])GetValue(IssuesInFileProperty);
        set => SetValue(IssuesInFileProperty, value);
    }

    public Issue SelectedIssue
    {
        get => (Issue)GetValue(SelectedIssueProperty);
        set => SetValue(SelectedIssueProperty, value);
    }

    private static void OnSourceCodePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SourceCodeEditor sourceCodeEditorControl)
        {
            sourceCodeEditorControl.LoadSourceCode();
        }
    }

    private static void OnSelectedIssueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SourceCodeEditor sourceCodeEditorControl && e.NewValue is Issue issue)
        {
            sourceCodeEditorControl.HighlightSelectedIssue(issue);
        }
    }

    private void LoadSourceCode()
    {
        if (!string.IsNullOrEmpty(SourceCodePath))
        {
            try
            {
                Load(SourceCodePath);
                SetSyntaxHighlighting();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading source code: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Document.Text = string.Empty;
            }
        }
        else
        {
            Document.Text = string.Empty;
        }
    }

    private void SetSyntaxHighlighting()
    {
        string fileExtension = Path.GetExtension(SourceCodePath);
        string syntaxName = string.Empty;

        switch (fileExtension.ToLower())
        {
            case ".cs":
                syntaxName = "C#";
                break;
            case ".vb":
                syntaxName = "VB";
                break;
        }

        SyntaxHighlighting = !string.IsNullOrEmpty(syntaxName)
            ? HighlightingManager.Instance.GetDefinition(syntaxName)
            : null;
    }

    private void HighlightSelectedIssue(Issue issue)
    {
        if (issue != null && issue.Location != null && issue.Location.Any())
        {
            var location = issue.Location.FirstOrDefault(loc => SourceCodePath.EndsWith(loc.Uri));
            if (location != null)
            {
                var region = location.Region;
                if (region != null)
                {
                    ScrollTo(region.StartLine, region.StartColumn);
                    var start = new TextLocation(region.StartLine, region.StartColumn);
                    var end = new TextLocation(region.EndLine, region.EndColumn);
                    int startOffset = Document.GetOffset(start);
                    int endOffset = Document.GetOffset(end);
                    SelectionStart = startOffset;
                    SelectionLength = endOffset - startOffset;
                }
            }
        }
        else
        {
            SelectionStart = 0;
            SelectionLength = 0;
        }
    }
}