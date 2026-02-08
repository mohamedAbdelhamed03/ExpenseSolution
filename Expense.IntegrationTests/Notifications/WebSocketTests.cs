using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Expense.API;
using Expense.IntegrationTests.Helpers;
using Xunit;
using FluentAssertions;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Shared;
using System.Net.Http.Json;
using System.Collections.Generic;

namespace Expense.IntegrationTests.Notifications
{
    public class WebSocketTests : IntegrationTestBase
    {
        public WebSocketTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Connect_WithValidToken_ShouldSucceed()
        {
            // Arrange
            var token = await AuthenticateAsync();
            var client = _factory.Server.CreateWebSocketClient();
            var uri = new Uri(_factory.Server.BaseAddress, $"/ws/notifications?access_token={token}");

            // Act
            using var ws = await client.ConnectAsync(uri, CancellationToken.None);

            // Assert
            ws.State.Should().Be(WebSocketState.Open);
        }

        [Fact]
        public async Task Connect_WithoutToken_ShouldFail()
        {
            // Arrange
            var client = _factory.Server.CreateWebSocketClient();
            var uri = new Uri(_factory.Server.BaseAddress, "/ws/notifications");

            // Act & Assert
            // WebSocketClient.ConnectAsync throws InvalidOperationException when handshake fails (401)
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConnectAsync(uri, CancellationToken.None));
            ex.Message.Should().Contain("401");
        }

        [Fact]
        public async Task Connect_WithInvalidToken_ShouldFail()
        {
            // Arrange
            var client = _factory.Server.CreateWebSocketClient();
            var uri = new Uri(_factory.Server.BaseAddress, "/ws/notifications?access_token=invalid_token");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConnectAsync(uri, CancellationToken.None));
            ex.Message.Should().Contain("401");
        }
    }
}
