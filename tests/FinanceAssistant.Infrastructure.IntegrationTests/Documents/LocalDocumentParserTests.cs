using System.Text;
using FinanceAssistant.Application.Documents;
using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Infrastructure.Documents;

namespace FinanceAssistant.Infrastructure.IntegrationTests.Documents;

public sealed class LocalDocumentParserTests
{
    [Fact]
    public async Task ParsesPlainTextAsUtf8()
    {
        var parser = new LocalDocumentParser();

        var result = await parser.ParseAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("hello\nworld")),
            DocumentMediaTypes.PlainText);

        Assert.Equal(DocumentMediaTypes.PlainText, result.VerifiedMediaType);
        Assert.Equal("hello\nworld", result.UntrustedExtractedText);
        Assert.Null(result.PdfPageCount);
    }

    [Fact]
    public async Task RejectsInvalidUtf8Text()
    {
        var parser = new LocalDocumentParser();

        var exception = await Assert.ThrowsAsync<DocumentParseException>(
            () => parser.ParseAsync(new MemoryStream([0xff]), DocumentMediaTypes.PlainText));

        Assert.Equal("Plain text document must be valid UTF-8.", exception.Message);
    }

    [Fact]
    public async Task RejectsTextWithNulBytes()
    {
        var parser = new LocalDocumentParser();

        var exception = await Assert.ThrowsAsync<DocumentParseException>(
            () => parser.ParseAsync(
                new MemoryStream(Encoding.UTF8.GetBytes("hello\0world")),
                DocumentMediaTypes.PlainText));

        Assert.Equal("Plain text document must not contain NUL bytes.", exception.Message);
    }

    [Fact]
    public async Task RejectsPdfWithoutSignature()
    {
        var parser = new LocalDocumentParser();

        var exception = await Assert.ThrowsAsync<DocumentParseException>(
            () => parser.ParseAsync(
                new MemoryStream(Encoding.UTF8.GetBytes("not a pdf")),
                DocumentMediaTypes.Pdf));

        Assert.Equal("PDF document signature is invalid.", exception.Message);
    }

    [Fact]
    public async Task RejectsPdfBytesDeclaredAsText()
    {
        var parser = new LocalDocumentParser();

        var exception = await Assert.ThrowsAsync<DocumentParseException>(
            () => parser.ParseAsync(
                new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7")),
                DocumentMediaTypes.PlainText));

        Assert.Equal("Declared media type does not match document content.", exception.Message);
    }
}
