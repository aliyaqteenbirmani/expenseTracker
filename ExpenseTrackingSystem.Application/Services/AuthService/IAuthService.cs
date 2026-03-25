using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;

namespace ExpenseTrackingSystem.Application.Services.AuthService
{
    public interface IAuthService
    {
        public Task<ApiResponse<User>> RegisterUser(UserDto userDto);
        public Task<ApiResponse<LoginResponseDto>> LoginUser(LoginDto loginDto);
    }
}
