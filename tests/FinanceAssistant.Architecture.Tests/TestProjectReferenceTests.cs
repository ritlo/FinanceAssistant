
namespace FinanceAssistant.Architecture.Tests;

public sealed class TestProjectReferenceTests
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

    private static readonly string DomainTestsPath = Path.GetFullPath(
        Path.Combine(RepositoryRoot, "tests/FinanceAssistant.Domain.Tests/FinanceAssistant.Domain.Tests.csproj"));
    private static readonly string ApplicationTestsPath = Path.GetFullPath(
        Path.Combine(RepositoryRoot, "tests/FinanceAssistant.Application.Tests/FinanceAssistant.Application.Tests.csproj"));
    private static readonly string InfrastructureTestsPath = Path.GetFullPath(
        Path.Combine(RepositoryRoot, "tests/FinanceAssistant.Infrastructure.IntegrationTests/FinanceAssistant.Infrastructure.IntegrationTests.csproj"));
    private static readonly string WebTestsPath = Path.GetFullPath(
        Path.Combine(RepositoryRoot, "tests/FinanceAssistant.Web.Tests/FinanceAssistant.Web.Tests.csproj"));
    private static readonly string ArchitectureTestsPath = Path.GetFullPath(
        Path.Combine(RepositoryRoot, "tests/FinanceAssistant.Architecture.Tests/FinanceAssistant.Architecture.Tests.csproj"));

    private static readonly ProjectPath DomainRef = new(DomainPath);
    private static readonly ProjectPath ApplicationRef = new(ApplicationPath);
    private static readonly ProjectPath InfrastructureRef = new(InfrastructurePath);
    private static readonly ProjectPath WebRef = new(WebPath);

    [Fact]
    public void DomainTests_ReferenceOnlyDomain()
    {
        var refs = ProjectMetadata.ParseProjectReferences(DomainTestsPath);
        Assert.Single(refs.References);
        Assert.Contains(DomainRef, refs.References);
    }

    [Fact]
    public void ApplicationTests_ReferenceOnlyApplication()
    {
        var refs = ProjectMetadata.ParseProjectReferences(ApplicationTestsPath);
        Assert.Single(refs.References);
        Assert.Contains(ApplicationRef, refs.References);
    }

    [Fact]
    public void InfrastructureTests_ReferenceOnlyInfrastructure()
    {
        var refs = ProjectMetadata.ParseProjectReferences(InfrastructureTestsPath);
        Assert.Single(refs.References);
        Assert.Contains(InfrastructureRef, refs.References);
    }

    [Fact]
    public void WebTests_ReferenceOnlyWeb()
    {
        var refs = ProjectMetadata.ParseProjectReferences(WebTestsPath);
        Assert.Single(refs.References);
        Assert.Contains(WebRef, refs.References);
    }

    [Fact]
    public void ArchitectureTests_HasNoProductionReferences()
    {
        var refs = ProjectMetadata.ParseProjectReferences(ArchitectureTestsPath);
        Assert.Empty(refs.References);
    }
}
