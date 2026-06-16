using System.Text;
using FinanceAssistant.Application.Documents;
using FinanceAssistant.Domain.Documents;
using UglyToad.PdfPig;

namespace FinanceAssistant.Infrastructure.Documents;

public sealed class LocalDocumentParser : IDocumentParser
{
    public async Task<DocumentParseResult> ParseAsync(
        Stream content,
        string declaredMediaType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        return declaredMediaType switch
        {
            DocumentMediaTypes.PlainText => await ParsePlainTextAsync(content, cancellationToken),
            DocumentMediaTypes.Pdf => await ParsePdfAsync(content, cancellationToken),
            _ => throw new DocumentParseException("Document media type is not supported."),
        };
    }

    private static async Task<DocumentParseResult> ParsePlainTextAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        var bytes = await ReadAllBytesAsync(content, cancellationToken);
        if (HasPdfSignature(bytes))
        {
            throw new DocumentParseException("Declared media type does not match document content.");
        }

        string text;
        try
        {
            text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new DocumentParseException("Plain text document must be valid UTF-8.");
        }

        if (text.Contains('\0', StringComparison.Ordinal))
        {
            throw new DocumentParseException("Plain text document must not contain NUL bytes.");
        }

        return new DocumentParseResult(DocumentMediaTypes.PlainText, text, PdfPageCount: null);
    }

    private static async Task<DocumentParseResult> ParsePdfAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        var bytes = await ReadAllBytesAsync(content, cancellationToken);
        if (!HasPdfSignature(bytes))
        {
            throw new DocumentParseException("PDF document signature is invalid.");
        }

        try
        {
            using var document = PdfDocument.Open(bytes);
            if (document.NumberOfPages > ParsedDocument.MaximumPdfPageCount)
            {
                throw new DocumentParseException("PDF document exceeds the 100-page limit.");
            }

            var builder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(page.Text);
            }

            return new DocumentParseResult(DocumentMediaTypes.Pdf, builder.ToString(), document.NumberOfPages);
        }
        catch (DocumentParseException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new DocumentParseException("PDF document could not be parsed.");
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream content, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }

    private static bool HasPdfSignature(byte[] bytes)
    {
        return bytes.Length >= 5 && Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-";
    }
}
