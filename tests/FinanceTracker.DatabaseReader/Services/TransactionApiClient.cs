using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using FinanceTracker.DatabaseReader.Models;

namespace FinanceTracker.DatabaseReader.Services
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
            var userId = _configuration["UserId"] ?? "DefaultUser-1";
            var url = $"api/transactions?userId={userId}";
            return await _httpClient.GetFromJsonAsync<List<Transaction>>(url) ?? new List<Transaction>();
        }
    }
}
