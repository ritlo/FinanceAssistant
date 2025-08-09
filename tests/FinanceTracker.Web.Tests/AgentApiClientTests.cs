using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using Moq.Protected;
using Xunit;
using FinanceTracker.Web.Services;

namespace FinanceTracker.Web.Tests
{
    public class AgentApiClientTests
    {
        [Fact]
        public async Task ProcessAgentRequestAsync_ReturnsSuccess_WhenApiReturnsSuccess()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"result\":\"ok\"}")
                });
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger<AgentApiClient>>();
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["UserId"]).Returns("test-user");
            var client = new AgentApiClient(httpClient, loggerMock.Object, configMock.Object);

            // Act
            var result = await client.ProcessAgentRequestAsync("test");

            // Assert
            Assert.True(result.Success);
            Assert.Contains("ok", result.Content);
        }

        [Fact]
        public async Task ProcessAgentRequestAsync_ReturnsError_WhenApiReturnsError()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("error")
                });
            var httpClient = new HttpClient(handlerMock.Object);
            var loggerMock = new Mock<ILogger<AgentApiClient>>();
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["UserId"]).Returns("test-user");
            var client = new AgentApiClient(httpClient, loggerMock.Object, configMock.Object);

            // Act
            var result = await client.ProcessAgentRequestAsync("test");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("error", result.ErrorMessage);
        }
    }
}
