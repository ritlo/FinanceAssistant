using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Domain.Documents;

public readonly record struct DocumentId
{
    public DocumentId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException("Document ID is required.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static DocumentId New() => new(Guid.NewGuid());
}
