        // ...existing code...
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using FinanceTracker.Web.Models;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Web.Services
{
    public class TransactionApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public TransactionApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<Transaction>> GetTransactions()
        {
            var userId = _configuration["UserId"];
            var url = $"api/transactions?userId={userId}";
            return await _httpClient.GetFromJsonAsync<List<Transaction>>(url) ?? new List<Transaction>();
        }

        public async Task<List<MonthlySummaryItem>> GetMonthlySummary(int month, int year)
        {
            var userId = _configuration["UserId"];
            var url = $"api/transactions/summary/monthly?userId={userId}&month={month}&year={year}";
            return await _httpClient.GetFromJsonAsync<List<MonthlySummaryItem>>(url) ?? new List<MonthlySummaryItem>();
        }
    }
}
