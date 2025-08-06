using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using IronPdf;
using System.IO;
using System;

namespace FinanceTracker.ApiService.Models.Plugins
{
    public class DocumentParsingPlugin
    {
        private readonly ILogger<DocumentParsingPlugin> _logger;

        public DocumentParsingPlugin(ILogger<DocumentParsingPlugin> logger)
        {
            _logger = logger;
        }

        [KernelFunction, Description("Parses a document from a stream and extracts text content.")]
        public string ParseDocument(
            [Description("The stream containing the document data.")] Stream documentStream,
            [Description("The name of the file being processed.")] string fileName)
        {
            _logger.LogInformation("Parsing document: {FileName}", fileName);

            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (extension != ".pdf")
            {
                _logger.LogWarning("Unsupported document type: {FileName}", fileName);
                return $"Unsupported document type: {extension}. This function currently only supports PDF files.";
            }

            try
            {
                using var memoryStream = new MemoryStream();
                documentStream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                var pdfDocument = new PdfDocument(memoryStream);
                var text = pdfDocument.ExtractAllText();
                _logger.LogInformation("Successfully extracted text from PDF: {FileName}", fileName);
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while parsing the document: {FileName}", fileName);
                return $"An unexpected error occurred while parsing the document: {ex.Message}";
            }
        }
    }
}
