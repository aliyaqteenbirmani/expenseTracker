using Microsoft.EntityFrameworkCore;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.AuthDtos;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Domain.Enums;
using SpendwiseSystem.Infrastructure.Data.DbContext;
using SpendwiseSystem.Infrastructure.Data.Migrations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDapperContext _dapperContext;
        private readonly AppDbContext _context;

        public AuthRepository(IDapperContext dbContext, AppDbContext context)
        {
            _dapperContext = dbContext;
            _context = context;
        }

        public async Task<ApiResponse<LoginResponseDbo>> Login(LoginDto loginDto)
        {
            var userFromDb = await _dapperContext.GetSingleAsync<LoginResponseDbo>(
                "SP_GetUserByEmail",
                new { Email = loginDto.UserName });

            if (!userFromDb.Success)
            {
                return new ApiResponse<LoginResponseDbo>
                {
                    Success = false,
                    Message = "Invalid UserName",
                };
            }

            return new ApiResponse<LoginResponseDbo>
            {
                Success = true,
                Message = "Login successful",
                Data = userFromDb
            };

        }

        public async Task<ApiResponse<RefreshTokenWithUserDataDto>> RefreshTokenWithUser(RefreshTokenModel refreshToken)
        {
            var raw = await _dapperContext.GetSingleAsync<ApiResponseRaw>("SP_GetRefreshTokenWithUser", refreshToken);
            if (!raw.Success)
            {
                return new ApiResponse<RefreshTokenWithUserDataDto>
                {
                    Success = raw.Success,
                    Message = raw?.Message ?? "Invalid Refresh Token",
                    Data = null
                };
            }

            var data = string.IsNullOrWhiteSpace(raw.Data)
                ? null
                : JsonSerializer.Deserialize<RefreshTokenWithUserDataDto>(raw.Data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new ApiResponse<RefreshTokenWithUserDataDto>
            {
                Success = true,
                Message = raw.Message,
                Data = data
            };
        }

        public async Task<ApiResponse<User>> RegisterUser(User user)
        {
            var parameters = new
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                PasswordHash = user.PasswordHash,
                PasswordSalt = user.PasswordSalt,
                CreatedBy = user.CreatedBy,
                Gender = user.Gender
            };

            try
            {
                var resultFromDb = await _dapperContext.GetSingleAsync<ApiResponse<User>>("SP_RegisterUserAndUserRole", parameters);
                return resultFromDb;
            }
            catch (Exception ex)
            {
                return new ApiResponse<User>
                {
                    Success = false,
                    Message = $"{ex.Message} {(ex.InnerException != null ? ex.InnerException.Message : string.Empty)}"
                };
            }
        }

        public async Task SaveRefreshToken(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

        }

        public async Task<List<string>> GetUserRoles(Guid userId)
        {
            var roles = await _dapperContext.GetListAsync<string>(
                "SP_GetUserRolesByUserId",
                new { UserId = userId }
            );

            return roles;
        }

        public async Task<bool> IsUserExist(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<ApiResponse<bool>> SavePasswordResetToken(string email, string hashedToken, DateTime expiresOn)
        {
            await _context.Users.Where(u => u.Email == email)
                  .ExecuteUpdateAsync(setters => setters
                  .SetProperty(u => u.ResetToken, hashedToken)
                  .SetProperty(u => u.ResetTokenExpiry, expiresOn));

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Password reset token saved successfully",
                Data = true
            };
        }

        public async Task<ApiResponseRaw> ResetPassword(string email, byte[] hashedToken, byte[] passwordHash, byte[] saltKey)
        {
            var fromDb = await _dapperContext.GetSingleAsync<ApiResponseRaw>(
                "SP_ResetUserPassword",
                new { Email = email,
                      PasswordHash = passwordHash,
                      PasswordSalt = saltKey,
                      ResetToken = hashedToken});
            return fromDb;
        }
    }
}



