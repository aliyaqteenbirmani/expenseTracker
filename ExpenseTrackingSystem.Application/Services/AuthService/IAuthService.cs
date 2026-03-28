using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Services.AuthService
{
    public interface IAuthService
    {
        public Task<ApiResponse<User>> RegisterUser(UserDto userDto);
        public Task<ApiResponse<LoginResponseDto>> LoginUser(LoginDto loginDto);
    }
}


