using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SarifViewer.Models;

public enum IssueState
{
    Expected,
    Actual,
    New,
    Lost,
    Modified
}

public enum IssueLanguage
{
    CSharpAndVisualBasic,
    CSharp,
    VisualBasic
}

public class ValueWithDisplayName<T>
{
    public T Value { get; set; }
    public string DisplayName { get; set; } = "";
}

public static class CollectionExtensions
{
    public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}