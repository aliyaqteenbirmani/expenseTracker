using ExpenseTrackingSystem.Application.Interfaces;
using ExpenseTrackingSystem.Domain.DBOs;
using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;
using ExpenseTrackingSystem.Infrastructure.Data.DbContext;
using System.Security.Cryptography;
using System.Text;

namespace ExpenseTrackingSystem.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDapperContext _dapperContext;

        public AuthRepository(IDapperContext dbContext)
        {
            _dapperContext = dbContext;
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

            using var hmac = new HMACSHA256(userFromDb.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
            return new ApiResponse<LoginResponseDbo>
            {
                Success = true,
                Message = "Login successful",
                Data = userFromDb
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
                Gender = (int)user.Gender
            };

            try
            {
                var resultFromDb = await _dapperContext.GetSingleAsync<ApiResponse<User>>("SP_AddUser", parameters);
                return resultFromDb;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while registering the user: {ex.Message}");
                return new ApiResponse<User>
                {
                    Success = false,
                    Message = $"{ex.Message} {(ex.InnerException != null ? ex.InnerException.Message : string.Empty)}"
                };
            }
        }
    }
}
