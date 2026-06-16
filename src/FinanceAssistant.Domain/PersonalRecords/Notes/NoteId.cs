using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Domain.PersonalRecords.Notes;

public readonly record struct NoteId
{
    public NoteId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException("Note ID is required.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static NoteId New() => new(Guid.NewGuid());
}
