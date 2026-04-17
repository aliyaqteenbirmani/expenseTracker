using Microsoft.AspNetCore.Identity;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Services.TokenService
{
    public interface ITokenService
    {
        //string GenerateJwtToken(UserDataForTokenGeneration user);
        Task<(string token, DateTime expires)> CreateAccessTokenAsync(UserDataForTokenGeneration user,List<string> roles);
        
    }
}


