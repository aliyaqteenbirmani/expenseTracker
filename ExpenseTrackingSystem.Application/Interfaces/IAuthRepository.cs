using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTrackingSystem.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<ApiResponse<User>> RegisterUser(User user);
        Task<ApiResponse<User>> Login(LoginDto loginDto);

    }
}
