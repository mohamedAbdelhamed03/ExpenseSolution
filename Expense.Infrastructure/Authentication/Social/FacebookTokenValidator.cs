using System.Net.Http.Json;
using Expense.Core.Abstractions.Authentication;
using Expense.Core.Domain.IdentityEntities;
using Expense.Core.DTOs.Auth;
using Newtonsoft.Json;

namespace Expense.Infrastructure.Authentication.Social
{
    public class FacebookTokenValidator : ISocialTokenValidator
    {
        private readonly HttpClient _httpClient;

        public FacebookTokenValidator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public AuthProvider Provider => AuthProvider.Facebook;

        public async Task<SocialUserDto?> ValidateTokenAsync(string token)
        {
            try
            {
                // Verify token and get user info
                var response = await _httpClient.GetAsync($"https://graph.facebook.com/me?fields=id,email,first_name,last_name,picture.type(large)&access_token={token}");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                var fbUser = JsonConvert.DeserializeObject<FacebookUserResponse>(content);

                if (fbUser == null || string.IsNullOrEmpty(fbUser.Id))
                    return null;

                // Requirement: Handle Facebook accounts without email
                // Either reject or require later. We will return user without email if missing, 
                // but the service will reject it based on requirements "Prevent duplicate users by email" implies email is central.
                // The prompt says "Handle Facebook accounts without email: Either reject login with clear error Or require client to complete email later".
                // I will return the DTO. If email is null, the service can decide to reject.

                return new SocialUserDto
                {
                    Id = fbUser.Id,
                    Email = fbUser.Email ?? string.Empty, // Service will check this
                    FirstName = fbUser.FirstName,
                    LastName = fbUser.LastName,
                    PictureUrl = fbUser.Picture?.Data?.Url
                };
            }
            catch
            {
                return null;
            }
        }

        private class FacebookUserResponse
        {
            [JsonProperty("id")]
            public string Id { get; set; } = string.Empty;
            
            [JsonProperty("email")]
            public string? Email { get; set; }
            
            [JsonProperty("first_name")]
            public string FirstName { get; set; } = string.Empty;
            
            [JsonProperty("last_name")]
            public string LastName { get; set; } = string.Empty;

            [JsonProperty("picture")]
            public FacebookPictureData? Picture { get; set; }
        }

        private class FacebookPictureData
        {
            [JsonProperty("data")]
            public FacebookPictureUrl? Data { get; set; }
        }

        private class FacebookPictureUrl
        {
            [JsonProperty("url")]
            public string? Url { get; set; }
        }
    }
}
