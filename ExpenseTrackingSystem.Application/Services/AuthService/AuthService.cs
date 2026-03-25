using ExpenseTrackingSystem.Application.Interfaces;
using ExpenseTrackingSystem.Application.Services.TokenService;
using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;
using ExpenseTrackingSystem.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace ExpenseTrackingSystem.Application.Services.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly ITokenService _tokenService;

        public AuthService(IAuthRepository authRepository, ITokenService tokenService)
        {
            _authRepository = authRepository;
            _tokenService = tokenService;
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginUser(LoginDto loginDto)
        {
            var responseFromRepo = await _authRepository.Login(loginDto);
            if (!responseFromRepo.Success)
            {
                return new ApiResponse<LoginResponseDto>
                {
                    Success = false,
                    Message = "Invalid login credentials.",
                    Data = null,
                };
            }
            var userDataForToken = new UserDataForTokenGeneration
            {
                Id = responseFromRepo.Data.Id,
                FirstName = responseFromRepo.Data.FirstName,
                LastName = responseFromRepo.Data.LastName,
                Email = responseFromRepo.Data.Email,
                Phone = responseFromRepo.Data.Phone,
            };
            var token = _tokenService.GenerateJwtToken(userDataForToken);
            var userResponse = new LoginResponseDto
            {
                Id = responseFromRepo.Data.Id,
                FirstName = responseFromRepo.Data.FirstName,
                LastName = responseFromRepo.Data.LastName,
                Email = responseFromRepo.Data.Email,
                Phone = responseFromRepo.Data.Phone,
                Gender = (Gender)responseFromRepo.Data.Gender,
                Token = token,
            };
            return new ApiResponse<LoginResponseDto>
            {
                Success = true,
                Message = "Login successful.",
                Data = userResponse,
            };
        }

        public async Task<ApiResponse<User>> RegisterUser(UserDto userDto)
        {
            var hmac = new HMACSHA256();
            var user = new User
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Gender = userDto.Gender,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(userDto.Password)),
                PasswordSalt = hmac.Key,
                Phone = userDto.Phone,
                ResetToken = null,
                ResetTokenExpiry = null,
                CreatedBy = userDto.FirstName+" "+userDto.LastName,
            };
            var result =  await _authRepository.RegisterUser(user);
            return result;
        }
    }
}
