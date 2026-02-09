using Expense.Core.Abstractions.Authentication;
using Expense.Core.Domain.Enums;
using Expense.Core.Domain.IdentityEntities;
using Expense.Core.DTOs.Auth;
using Google.Apis.Auth;

namespace Expense.Infrastructure.Authentication.Social
{
    public class GoogleTokenValidator : ISocialTokenValidator
    {
        public AuthProvider Provider => AuthProvider.Google;

        public async Task<SocialUserDto?> ValidateTokenAsync(string token)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(token);
                
                return new SocialUserDto
                {
                    Id = payload.Subject,
                    Email = payload.Email,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    PictureUrl = payload.Picture
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
