
namespace FinanceAssistant.Architecture.Tests;

public sealed class ProductionProjectReferenceTests
{
    private static readonly string RepositoryRoot = ProjectMetadata.FindRepositoryRoot(
        AppContext.BaseDirectory);

    private static readonly string DomainPath = Path.GetFullPath(
        Path.Combine(RepositoryRoot, "src/FinanceAssistant.Domain/FinanceAssistant.Domain.csproj"));
    private static readonly string ApplicationPath = Path.GetFullPath(
        Path.Combine(RepositoryRoot, "src/FinanceAssistant.Application/FinanceAssistant.Application.csproj"));
    private static readonly string InfrastructurePath = Path.GetFullPath(
        Path.Combine(RepositoryRoot, "src/FinanceAssistant.Infrastructure/FinanceAssistant.Infrastructure.csproj"));
    private static readonly string WebPath = Path.GetFullPath(
        Path.Combine(RepositoryRoot, "src/FinanceAssistant.Web/FinanceAssistant.Web.csproj"));

    private static readonly ProjectPath DomainRef = new(DomainPath);
    private static readonly ProjectPath ApplicationRef = new(ApplicationPath);
    private static readonly ProjectPath InfrastructureRef = new(InfrastructurePath);
    private static readonly ProjectPath WebRef = new(WebPath);

    [Fact]
    public void Domain_HasNoProjectReferences()
    {
        var refs = ProjectMetadata.ParseProjectReferences(DomainPath);
        Assert.Empty(refs.References);
    }

    [Fact]
    public void Application_ReferenceOnlyDomain()
    {
        var refs = ProjectMetadata.ParseProjectReferences(ApplicationPath);
        Assert.Single(refs.References);
        Assert.Contains(DomainRef, refs.References);
    }

    [Fact]
    public void Infrastructure_ReferenceApplicationAndDomain()
    {
        var refs = ProjectMetadata.ParseProjectReferences(InfrastructurePath);
        Assert.Equal(2, refs.References.Count);
        Assert.Contains(DomainRef, refs.References);
        Assert.Contains(ApplicationRef, refs.References);
    }

    [Fact]
    public void Web_ReferenceApplicationAndInfrastructure()
    {
        var refs = ProjectMetadata.ParseProjectReferences(WebPath);
        Assert.Equal(2, refs.References.Count);
        Assert.Contains(ApplicationRef, refs.References);
        Assert.Contains(InfrastructureRef, refs.References);
    }

    [Fact]
    public void NoProductionProjectReferencesTestProjects()
    {
        var testPaths = new[]
        {
            RepositoryRoot + "/tests/FinanceAssistant.Domain.Tests/",
            RepositoryRoot + "/tests/FinanceAssistant.Application.Tests/",
            RepositoryRoot + "/tests/FinanceAssistant.Infrastructure.IntegrationTests/",
            RepositoryRoot + "/tests/FinanceAssistant.Web.Tests/",
            RepositoryRoot + "/tests/FinanceAssistant.Architecture.Tests/"
        };

        var productionProjects = new[] { DomainPath, ApplicationPath, InfrastructurePath, WebPath };

        foreach (var projectPath in productionProjects)
        {
            var refs = ProjectMetadata.ParseProjectReferences(projectPath);
            foreach (var testDir in testPaths)
            {
                foreach (var refPath in refs.References)
                {
                    Assert.False(refPath.Absolute.StartsWith(testDir, StringComparison.OrdinalIgnoreCase),
                        $"{projectPath} references a path under tests/: {refPath.Absolute}");
                }
            }
        }
    }

    [Fact]
    public void NoProductionProjectReferencesLegacy()
    {
        var legacyPath = Path.GetFullPath(Path.Combine(RepositoryRoot, "FinanceTracker"));

        var productionProjects = new[] { DomainPath, ApplicationPath, InfrastructurePath, WebPath };

        foreach (var projectPath in productionProjects)
        {
            var refs = ProjectMetadata.ParseProjectReferences(projectPath);
            foreach (var refPath in refs.References)
            {
                Assert.False(refPath.Absolute.StartsWith(legacyPath, StringComparison.OrdinalIgnoreCase),
                    $"{projectPath} references a path under FinanceTracker/: {refPath.Absolute}");
            }
        }
    }
}
