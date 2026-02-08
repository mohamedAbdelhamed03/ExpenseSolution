using Expense.API;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading.Tasks;
using Expense.Core.DTOs.Auth;
using Expense.Core.DTOs.Shared;
using System.Net.Http.Json;

namespace Expense.IntegrationTests.Helpers
{
    public class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        protected readonly CustomWebApplicationFactory<Program> _factory;
        protected readonly HttpClient _client;

        public IntegrationTestBase(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        protected async Task<string> AuthenticateAsync(string email = "test@example.com", string password = "Password123!")
        {
            // Register first
            await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto 
            { 
                Email = email, 
                Password = password,
                ConfirmPassword = password,
                FirstName = "Test",
                LastName = "User"
            });

            // Login
            var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto 
            { 
                Email = email, 
                Password = password 
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Login failed with status {response.StatusCode}. Content: {errorContent}");
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<APIResponse<LoginResponseDto>>();
            var token = result?.Data?.AccessToken;
            
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return token ?? string.Empty;
        }
    }
}