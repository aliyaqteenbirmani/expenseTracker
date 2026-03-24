using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTrackingSystem.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> RegisterUser(User user);
        Task<UserApiResponse<User>> Login(LoginDto loginDto);

    }
}
