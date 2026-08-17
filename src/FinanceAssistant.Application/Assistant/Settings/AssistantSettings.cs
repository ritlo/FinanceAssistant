namespace FinanceAssistant.Application.Assistant.Settings;

public sealed record AssistantSettings(
    bool WriteProposalsEnabled,
    string EndpointUrl,
    int EndpointPort,
    bool AllowRemoteEndpoint)
{
    public const string DefaultEndpointUrl = "http://localhost/v1/chat/completions";
    public const int DefaultEndpointPort = 8080;

    public static AssistantSettings Default { get; } = new(
        WriteProposalsEnabled: true,
        EndpointUrl: DefaultEndpointUrl,
        EndpointPort: DefaultEndpointPort,
        AllowRemoteEndpoint: false);

    public static AssistantSettings FromConfiguredEndpoint(string? endpoint, bool allowRemoteEndpoint)
    {
        if (!TryParseEndpoint(endpoint, out var uri))
        {
            return Default with { AllowRemoteEndpoint = allowRemoteEndpoint };
        }

        return new AssistantSettings(
            WriteProposalsEnabled: true,
            EndpointUrl: EndpointUrlWithoutPort(uri),
            EndpointPort: uri.Port,
            AllowRemoteEndpoint: allowRemoteEndpoint);
    }

    public Uri BuildEndpointUri()
    {
        if (!TryParseEndpoint(EndpointUrl, out var uri) || EndpointPort is < 1 or > 65535)
        {
            return Default.BuildEndpointUri();
        }

        return new UriBuilder(uri)
        {
            Port = EndpointPort,
        }.Uri;
    }

    internal static bool TryParseEndpoint(string? endpoint, out Uri uri)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out uri!)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        uri = null!;
        return false;
    }

    internal static string EndpointUrlWithoutPort(Uri uri)
    {
        return new UriBuilder(uri)
        {
            Port = -1,
        }.Uri.ToString();
    }
}
