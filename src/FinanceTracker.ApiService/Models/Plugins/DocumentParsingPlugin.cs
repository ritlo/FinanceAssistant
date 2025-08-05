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

        [KernelFunction, Description("Extracts text from a PDF file, optionally using a password if the PDF is encrypted.")]
        public string ExtractTextFromPdf(
            [Description("The full path to the PDF file.")] string filePath,
            [Description("The password for the PDF file, if it is encrypted. Leave empty if not encrypted.")] string? password = null)
        {
            _logger.LogInformation("Extracting text from PDF: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found at '{FilePath}'", filePath);
                return $"Error: File not found at '{filePath}'.";
            }

            try
            {
                // Set the license key if you have one (for IronPDF)
                // License.LicenseKey = "YOUR_IRONPDF_LICENSE_KEY";

                PdfDocument pdfDocument = PdfDocument.FromFile(filePath, password);
                var text = pdfDocument.ExtractAllText();
                _logger.LogInformation("Successfully extracted text from PDF: {FilePath}", filePath);
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while parsing the PDF: {FilePath}", filePath);
                // Check if the exception message indicates a password error. This is a more robust way to handle it.
                if (ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase))
                {
                    return "Error: The provided password was incorrect, or the PDF is encrypted and no password was provided.";
                }
                return $"An unexpected error occurred while parsing the PDF: {ex.Message}";
            }
        }
    }
}
