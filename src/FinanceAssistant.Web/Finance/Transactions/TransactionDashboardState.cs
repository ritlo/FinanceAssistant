using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Categories.ListCategories;
using FinanceAssistant.Application.Finance.Summaries.GetMonthlySummary;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.GetTransactions;
using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Web.Finance.Transactions;

public sealed class TransactionDashboardState : IDisposable
{
    private readonly ListCategoriesUseCase listCategories;
    private readonly GetTransactionsUseCase getTransactions;
    private readonly GetMonthlySummaryUseCase getMonthlySummary;
    private readonly InProcessTransactionChangeNotifier transactionChangeNotifier;
    private readonly SemaphoreSlim reloadLock = new(1, 1);
    private bool disposed;

    public TransactionDashboardState(
        ListCategoriesUseCase listCategories,
        GetTransactionsUseCase getTransactions,
        GetMonthlySummaryUseCase getMonthlySummary,
        InProcessTransactionChangeNotifier transactionChangeNotifier)
    {
        this.listCategories = listCategories;
        this.getTransactions = getTransactions;
        this.getMonthlySummary = getMonthlySummary;
        this.transactionChangeNotifier = transactionChangeNotifier;

        var today = DateTime.Today;
        SelectedYear = today.Year;
        SelectedMonth = today.Month;

        this.transactionChangeNotifier.TransactionChanged += OnTransactionChangedAsync;
    }

    public event Func<Task>? StateChanged;

    public IReadOnlyList<CategoryResult> Categories { get; private set; } = [];

    public IReadOnlyList<TransactionResult> Transactions { get; private set; } = [];

    public GetMonthlySummaryResult? Summary { get; private set; }

    public string? SummaryErrorMessage { get; private set; }

    public int SelectedYear { get; private set; }

    public int SelectedMonth { get; private set; }

    public string SelectedMonthName => Summary is null
        ? string.Empty
        : new DateOnly(Summary.Year, Summary.Month, 1).ToString("MMMM");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await ReloadAsync(cancellationToken);
    }

    public async Task SetSummaryPeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        SelectedYear = year;
        SelectedMonth = month;
        await ReloadSummaryAsync(cancellationToken);
        await NotifyStateChangedAsync();
    }

    public string GetCategoryName(Guid categoryId)
    {
        return Categories.FirstOrDefault(category => category.Id == categoryId)?.Name ?? "Unknown";
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        transactionChangeNotifier.TransactionChanged -= OnTransactionChangedAsync;
        reloadLock.Dispose();
        disposed = true;
    }

    private async Task OnTransactionChangedAsync(CancellationToken cancellationToken)
    {
        await ReloadAsync(cancellationToken);
        await NotifyStateChangedAsync();
    }

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        await reloadLock.WaitAsync(cancellationToken);
        try
        {
            Categories = await listCategories.ExecuteAsync(cancellationToken);
            Transactions = await getTransactions.ExecuteAsync(cancellationToken);
            await ReloadSummaryCoreAsync(cancellationToken);
        }
        finally
        {
            reloadLock.Release();
        }
    }

    private async Task ReloadSummaryAsync(CancellationToken cancellationToken)
    {
        await reloadLock.WaitAsync(cancellationToken);
        try
        {
            await ReloadSummaryCoreAsync(cancellationToken);
        }
        finally
        {
            reloadLock.Release();
        }
    }

    private async Task ReloadSummaryCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            Summary = await getMonthlySummary.ExecuteAsync(
                new GetMonthlySummaryRequest(SelectedYear, SelectedMonth),
                cancellationToken);
            SummaryErrorMessage = null;
        }
        catch (DomainValidationException exception)
        {
            Summary = null;
            SummaryErrorMessage = exception.Message;
        }
    }

    private async Task NotifyStateChangedAsync()
    {
        if (StateChanged is null)
        {
            return;
        }

        foreach (var handler in StateChanged.GetInvocationList().Cast<Func<Task>>())
        {
            await handler();
        }
    }
}
