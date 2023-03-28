using System;
using System.Linq;

namespace SarifViewer.Models;

public class Region: IEquatable<Region>
{
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }

    public bool Equals(Region other) =>
        StartLine == other.StartLine
     && StartColumn == other.StartColumn
     && EndLine == other.EndLine
     && EndColumn == other.EndColumn;

    public override bool Equals(object obj)
    {
        return Equals(obj as Region);
    }

    public override int GetHashCode()
    {
        return StartLine * StartColumn * EndLine * EndColumn;
    }
}

public class Location: IEquatable<Location>
{
    public Region Region { get; set; }
    public string Uri { get; set;  }

    public bool Equals(Location other)
    {
        return other != null
            && other.Region.Equals(Region)
            && other.Uri.Equals(Uri);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Location);
    }

    public override int GetHashCode()
    {
        return Uri.Length * Region.GetHashCode();
    }
}

public class Issue: IEquatable<Issue>
{
    public string Id { get; set; }
    public Location[] Location { get; set; }
    public string Message { get; set; }
    public string JsonFilePath { get; set; }

    public string FirstLocationUri =>
        Location?.FirstOrDefault()?.Uri ?? "No location (project level issue)";

    public string FirstLocationUriAndLineNumber
    {
        get
        {
            var firstLoc = Location?.FirstOrDefault();
            var startLine = firstLoc?.Region?.StartLine;
            return (firstLoc == null || startLine == 0)
                ? "No location (project level issue)"
                : $"{firstLoc.Uri} (Line {startLine})";
        }
    }

    public string FirstLocationAsString()
    {
        var firstLocation = Location?.FirstOrDefault();
        return $"{firstLocation?.Uri ?? ""}_{firstLocation?.Region?.StartLine ?? 0}_{firstLocation?.Region?.StartColumn ?? 0}";
    }

    public bool Equals(Issue other)
    {
        return other != null
            && Id.Equals(other.Id)
            && Message.Equals(other.Message)
            && FirstLocationAsString() == FirstLocationAsString();
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Issue);
    }

    public override int GetHashCode()
    {
        return Location?.FirstOrDefault()?.GetHashCode() ?? 0;
    }
}