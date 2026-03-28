using ExpenseTrackingSystem.Domain.DBOs;
using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;

namespace ExpenseTrackingSystem.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<ApiResponse<User>> RegisterUser(User user);
        Task<ApiResponse<LoginResponseDbo>> Login(LoginDto loginDto);
    }
}
