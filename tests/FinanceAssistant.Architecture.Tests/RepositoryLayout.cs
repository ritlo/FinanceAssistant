using System.Linq;
using System.Xml.Linq;

namespace FinanceAssistant.Architecture.Tests;

public sealed class RepositoryLayout
{
    private static readonly string RepositoryRoot = ProjectMetadata.FindRepositoryRoot(
        AppContext.BaseDirectory);

    private static IEnumerable<string> EnumerateProjectFiles(string directory)
    {
        var normalizedDir = Path.GetFullPath(directory.TrimEnd('/', '\\'));
        return Directory.EnumerateDirectories(normalizedDir)
            .SelectMany(subdir => Directory.GetFiles(subdir, "*.csproj", SearchOption.TopDirectoryOnly))
            .Select(Path.GetFullPath)
            .ToList();
    }

    [Fact]
    public void ProductionProjects_EnumExactlyFour()
    {
        var actual = EnumerateProjectFiles(Path.Combine(RepositoryRoot, "src"))
            .Select(Path.GetFullPath)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var expected = new List<string>
        {
            Path.GetFullPath(Path.Combine(RepositoryRoot, "src/FinanceAssistant.Domain/FinanceAssistant.Domain.csproj")),
            Path.GetFullPath(Path.Combine(RepositoryRoot, "src/FinanceAssistant.Application/FinanceAssistant.Application.csproj")),
            Path.GetFullPath(Path.Combine(RepositoryRoot, "src/FinanceAssistant.Infrastructure/FinanceAssistant.Infrastructure.csproj")),
            Path.GetFullPath(Path.Combine(RepositoryRoot, "src/FinanceAssistant.Web/FinanceAssistant.Web.csproj"))
        }.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();

        Assert.Equal(expected, actual, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Solution_ContainsExactlyFourProductionAndFiveTestProjects()
    {
        var slnxPath = Path.Combine(RepositoryRoot, "FinanceAssistant.slnx");
        var doc = XDocument.Load(slnxPath);

        var solutionProjects = doc.Descendants()
            .Where(e => e.Name.LocalName == "Project")
            .Select(e => e.Attribute("Path")?.Value)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        var expectedProduction = new[]
        {
            "src/FinanceAssistant.Domain/FinanceAssistant.Domain.csproj",
            "src/FinanceAssistant.Application/FinanceAssistant.Application.csproj",
            "src/FinanceAssistant.Infrastructure/FinanceAssistant.Infrastructure.csproj",
            "src/FinanceAssistant.Web/FinanceAssistant.Web.csproj"
        };

        var expectedTest = new[]
        {
            "tests/FinanceAssistant.Domain.Tests/FinanceAssistant.Domain.Tests.csproj",
            "tests/FinanceAssistant.Application.Tests/FinanceAssistant.Application.Tests.csproj",
            "tests/FinanceAssistant.Infrastructure.IntegrationTests/FinanceAssistant.Infrastructure.IntegrationTests.csproj",
            "tests/FinanceAssistant.Web.Tests/FinanceAssistant.Web.Tests.csproj",
            "tests/FinanceAssistant.Architecture.Tests/FinanceAssistant.Architecture.Tests.csproj"
        };

        var expected = expectedProduction.Concat(expectedTest).OrderBy(p => p, StringComparer.Ordinal).ToList();

        Assert.Equal(expected, solutionProjects, StringComparer.Ordinal);
    }

    [Fact]
    public void Solution_NoUnapprovedProjects()
    {
        var slnxPath = Path.Combine(RepositoryRoot, "FinanceAssistant.slnx");
        var doc = XDocument.Load(slnxPath);

        var solutionProjects = doc.Descendants()
            .Where(e => e.Name.LocalName == "Project")
            .Select(e => e.Attribute("Path")?.Value)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        var approved = new HashSet<string>(new[]
        {
            "src/FinanceAssistant.Domain/FinanceAssistant.Domain.csproj",
            "src/FinanceAssistant.Application/FinanceAssistant.Application.csproj",
            "src/FinanceAssistant.Infrastructure/FinanceAssistant.Infrastructure.csproj",
            "src/FinanceAssistant.Web/FinanceAssistant.Web.csproj",
            "tests/FinanceAssistant.Domain.Tests/FinanceAssistant.Domain.Tests.csproj",
            "tests/FinanceAssistant.Application.Tests/FinanceAssistant.Application.Tests.csproj",
            "tests/FinanceAssistant.Infrastructure.IntegrationTests/FinanceAssistant.Infrastructure.IntegrationTests.csproj",
            "tests/FinanceAssistant.Web.Tests/FinanceAssistant.Web.Tests.csproj",
            "tests/FinanceAssistant.Architecture.Tests/FinanceAssistant.Architecture.Tests.csproj"
        });

        var unexpected = solutionProjects.Where(p => approved.Contains(p!) == false).ToList();
        Assert.Empty(unexpected);
    }

    private static Func<object, string> ActualFormatter(List<string> expected, List<string> actual)
    {
        return _ =>
            $"Expected {expected.Count} production projects but found {actual.Count}.\n" +
            $"Missing: {string.Join(", ", expected.Except(actual, StringComparer.OrdinalIgnoreCase))}\n" +
            $"Unexpected: {string.Join(", ", actual.Except(expected, StringComparer.OrdinalIgnoreCase))}";
    }
}
