
using System.Xml.Linq;

namespace FinanceAssistant.Architecture.Tests;

/// <summary>
/// Immutable representation of a project path.
/// </summary>
public sealed record ProjectPath(string Absolute);

/// <summary>
/// Immutable collection of normalized project-reference paths.
/// </summary>
public sealed record ProjectReferences(IReadOnlyCollection<ProjectPath> References)
{
    public static readonly ProjectReferences Empty = new([]);

    public bool HasUnexpected(ProjectPath unexpected) =>
        References.Contains(unexpected);

    public bool IsMissing(ProjectPath expected) =>
        !References.Contains(expected);
}

/// <summary>
/// Shared helper that walks from a base directory up to find the repository root.
/// </summary>
public static class ProjectMetadata
{
    /// <summary>
    /// Walks parent directories from <paramref name="baseDirectory"/> until it finds
    /// <c>FinanceAssistant.slnx</c>, then returns the repository root as an absolute path.
    /// </summary>
    public static string FindRepositoryRoot(string baseDirectory)
    {
        var current = Path.GetFullPath(baseDirectory);
        while (true)
        {
            var slnx = Path.Combine(current, "FinanceAssistant.slnx");
            if (File.Exists(slnx))
                return current;

            var parent = Directory.GetParent(current)?.FullName;
            if (string.IsNullOrEmpty(parent) || parent == current)
                throw new InvalidOperationException(
                    "Could not find FinanceAssistant.slnx; repository root is unreachable.");

            current = parent;
        }
    }

    /// <summary>
    /// Parses a project file and extracts every <c>ProjectReference Include</c> value,
    /// resolved relative to the project file's directory and normalized to an absolute path.
    /// </summary>
    public static ProjectReferences ParseProjectReferences(string projectPath)
    {
        var doc = XDocument.Load(projectPath);
        var projectDir = Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException(
            $"Could not determine directory for project file: {projectPath}");

        var references = doc.Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e =>
            {
                var include = e.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(include))
                    return null;

                var relative = include.TrimEnd('/', '\\').Replace('\\', '/');
                var absolute = Path.GetFullPath(Path.Combine(projectDir, relative));
                return new ProjectPath(absolute);
            })
            .OfType<ProjectPath>()
            .ToList();

        return new ProjectReferences(new HashSet<ProjectPath>(references, PathComparer.Instance));
    }
}

/// <summary>
/// Compares paths using the operating-system-appropriate comparer.
/// </summary>
internal sealed class PathComparer : IEqualityComparer<ProjectPath>
{
    public static readonly PathComparer Instance = new();

    public bool Equals(ProjectPath? x, ProjectPath? y)
    {
        if (x is null || y is null) return false;
        return string.Equals(x.Absolute, y.Absolute, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(ProjectPath obj) =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Absolute);
}
