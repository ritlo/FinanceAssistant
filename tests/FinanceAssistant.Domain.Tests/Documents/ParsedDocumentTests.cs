using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Tests.Documents;

public sealed class ParsedDocumentTests
{
    [Fact]
    public void CreateRejectsOversizedExtractedText()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => ParsedDocument.Create(
                DocumentId.New(),
                LocalProfileId.New(),
                DocumentMediaTypes.PlainText,
                new string('x', ParsedDocument.MaximumExtractedTextLength + 1),
                null,
                DateTimeOffset.UtcNow));

        Assert.Equal("Extracted document text exceeds the storage limit.", exception.Message);
    }

    [Fact]
    public void CreateRequiresPageCountOnlyForPdf()
    {
        var pdfException = Assert.Throws<DomainValidationException>(
            () => ParsedDocument.Create(
                DocumentId.New(),
                LocalProfileId.New(),
                DocumentMediaTypes.Pdf,
                "text",
                null,
                DateTimeOffset.UtcNow));
        var textException = Assert.Throws<DomainValidationException>(
            () => ParsedDocument.Create(
                DocumentId.New(),
                LocalProfileId.New(),
                DocumentMediaTypes.PlainText,
                "text",
                1,
                DateTimeOffset.UtcNow));

        Assert.Equal("PDF page count is required.", pdfException.Message);
        Assert.Equal("Page count is only supported for PDF documents.", textException.Message);
    }

    [Fact]
    public void CreateRejectsPdfAbovePageLimit()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => ParsedDocument.Create(
                DocumentId.New(),
                LocalProfileId.New(),
                DocumentMediaTypes.Pdf,
                "text",
                ParsedDocument.MaximumPdfPageCount + 1,
                DateTimeOffset.UtcNow));

        Assert.Equal("PDF document exceeds the 100-page limit.", exception.Message);
    }
}
