using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;

namespace ExpenseTrackingSystem.Application.Services.AuthService
{
    public interface IAuthService
    {
        public Task<bool> RegisterUser(UserDto userDto);
        public Task<UserApiResponse<LoginResponseDto>> LoginUser(LoginDto loginDto);
    }
}
