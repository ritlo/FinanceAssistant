using System.Globalization;
using System.Text;

namespace FinanceAssistant.Domain.Common;

internal static class RequiredText
{
    public static string Normalize(string value, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = CollapseWhitespace(value.Normalize(NormalizationForm.FormKC));

        if (normalized.Length == 0)
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return normalized;
    }

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var rune in value.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.SpaceSeparator || rune.Value is '\t' or '\n' or '\r')
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(rune);
        }

        return builder.ToString();
    }
}
