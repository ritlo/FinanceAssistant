using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using FinanceTracker.Web.Models;

namespace FinanceTracker.Web.Services
{
    public class TransactionApiClient
    {
        private readonly HttpClient _httpClient;

        public TransactionApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<MonthlySummaryItem>> GetMonthlySummary()
        {
            return await _httpClient.GetFromJsonAsync<List<MonthlySummaryItem>>("api/transactions/summary/monthly") ?? new List<MonthlySummaryItem>();
        }
    }
}
