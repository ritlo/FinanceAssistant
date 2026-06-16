namespace FinanceAssistant.Application.Finance.Summaries.GetMonthlySummary;

public sealed record GetMonthlySummaryResult(
    int Year,
    int Month,
    decimal ExpenseTotal,
    IReadOnlyList<MonthlySummaryCategoryResult> Categories);
