namespace FinanceAssistant.Web.Tests.Components.Pages;

public sealed class SettingsPageSourceTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);

    [Fact]
    public void SettingsPageExposesAssistantSettingsForm()
    {
        var source = ReadProjectFile("src/FinanceAssistant.Web/Components/Pages/Settings.razor");

        Assert.Contains("@page \"/settings\"", source, StringComparison.Ordinal);
        Assert.Contains("@inject GetAssistantSettingsUseCase GetSettings", source, StringComparison.Ordinal);
        Assert.Contains("@inject UpdateAssistantSettingsUseCase UpdateSettings", source, StringComparison.Ordinal);
        Assert.Contains("@inject IAssistantModelClient AssistantModelClient", source, StringComparison.Ordinal);
        Assert.Contains("@bind-Value=\"Form.WriteProposalsEnabled\"", source, StringComparison.Ordinal);
        Assert.Contains("@bind-Value=\"Form.EndpointUrl\"", source, StringComparison.Ordinal);
        Assert.Contains("@bind-Value=\"Form.EndpointPort\"", source, StringComparison.Ordinal);
        Assert.Contains("@bind-Value=\"Form.AllowRemoteEndpoint\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SettingsFormMapsToApplicationUpdateRequest()
    {
        var source = ReadProjectFile("src/FinanceAssistant.Web/Assistant/Settings/AssistantSettingsFormModel.cs");

        Assert.Contains("UpdateAssistantSettingsRequest", source, StringComparison.Ordinal);
        Assert.Contains("WriteProposalsEnabled", source, StringComparison.Ordinal);
        Assert.Contains("EndpointUrl", source, StringComparison.Ordinal);
        Assert.Contains("EndpointPort", source, StringComparison.Ordinal);
        Assert.Contains("AllowRemoteEndpoint", source, StringComparison.Ordinal);
        Assert.Contains("[Range(1, 65535)]", source, StringComparison.Ordinal);
    }

    private static string ReadProjectFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot, relativePath));
    }

    private static string FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FinanceAssistant.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
