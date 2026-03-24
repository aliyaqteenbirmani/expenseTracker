using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;

namespace ExpenseTrackingSystem.Application.Services.TokenService
{
    public interface ITokenService
    {
        string GenerateJwtToken(UserDataForTokenGeneration user);
    }
}
