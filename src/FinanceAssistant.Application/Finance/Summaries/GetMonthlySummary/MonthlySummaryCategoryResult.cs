namespace FinanceAssistant.Application.Finance.Summaries.GetMonthlySummary;

public sealed record MonthlySummaryCategoryResult(
    Guid CategoryId,
    string CategoryName,
    decimal Total);
