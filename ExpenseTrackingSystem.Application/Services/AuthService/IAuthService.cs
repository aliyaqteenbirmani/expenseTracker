using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.DTOs.AuthDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Services.AuthService
{
    public interface IAuthService
    {
        public Task<ApiResponse<User>> RegisterUser(UserDto userDto);
        public Task<ApiResponse<LoginResponseDto>> LoginUser(LoginDto loginDto);
        public Task<ApiResponse<AccessAndRefreshToken>> RefreshToken(RefreshTokenModel refreshToken);
        public Task<ApiResponse<bool>> GenerateResetLink(string email, string userName);
        public Task<ApiResponseRaw> ResetPassword(ResetPasswordDto resetPasswordDto);
        public Task<bool> IsUserExist(string email);

    }
}


