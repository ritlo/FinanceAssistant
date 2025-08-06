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

        public async Task<List<MonthlySummaryItem>> GetMonthlySummary()
        {
            var userId = _configuration["UserId"];
            return await _httpClient.GetFromJsonAsync<List<MonthlySummaryItem>>($"api/transactions/summary/monthly?userId={userId}") ?? new List<MonthlySummaryItem>();
        }
    }
}
