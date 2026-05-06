using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.AuthDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<ApiResponse<User>> RegisterUser(User user);
        Task<ApiResponse<LoginResponseDbo>> Login(LoginDto loginDto);
        Task SaveRefreshToken(RefreshToken refreshToken);
        Task<ApiResponse<RefreshTokenWithUserDataDto>> RefreshTokenWithUser(RefreshTokenModel refreshToken);
        Task<ApiResponse<bool>> SavePasswordResetToken(string email, string hashedToken, DateTime expiresOn);
        Task<ApiResponseRaw> ResetPassword(string email, byte[] hashedToken, byte[] passwordHash, byte[] saltKey);
        Task<List<string>> GetUserRoles(Guid userId);

        Task<bool> IsUserExist(string email);
    }
}


