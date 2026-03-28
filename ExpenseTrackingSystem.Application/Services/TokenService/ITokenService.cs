using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Services.TokenService
{
    public interface ITokenService
    {
        string GenerateJwtToken(UserDataForTokenGeneration user);
    }
}


