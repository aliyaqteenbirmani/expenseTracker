using ExpenseTrackingSystem.Application.Interfaces;
using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;
using ExpenseTrackingSystem.Infrastructure.Data.DbContext;
using Microsoft.AspNetCore.Mvc;
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


        public async Task<UserApiResponse<User>> Login(LoginDto loginDto)
        {
            var userFromDb = await _dapperContext.GetSingleAsync<User>("SP_GetUserByEmail", new 
            {   Email = loginDto.UserName
            });
           
            if (userFromDb is null)
            {
                return new UserApiResponse<User>
                {
                    Success = false,
                    Message = "Invalid UserName",
                };
            }
            using var hmac = new HMACSHA256(userFromDb.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
            if (!userFromDb.PasswordHash.SequenceEqual(computedHash))
            {
                return new UserApiResponse<User>
                {
                    Success = false,
                    Message = "Invalid Email or Password",
                };
            }
            return new UserApiResponse<User>
            {
                Success = true,
                Message = "Login successful",
                Data = userFromDb
            };
        }

        public async Task<bool> RegisterUser(User user)
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
                var resultFromDb = await _dapperContext.ExecuteAsync("SP_AddUser", parameters);
                return resultFromDb;
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework like Serilog, NLog, etc.)
                Console.WriteLine($"An error occurred while registering the user: {ex.Message}");
                return false;
            }
        }

        
    }
}
