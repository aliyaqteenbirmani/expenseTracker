using AutoMapper;
using Microsoft.Extensions.Options;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.EmailService;
using SpendwiseSystem.Application.Services.TokenService;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.DTOs.AuthDtos;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Domain.Enums;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SpendwiseSystem.Application.Services.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly AppLinkSettings _appLinkSettings;
        private readonly IMapper _mapper;

        public AuthService(IAuthRepository authRepository, ITokenService tokenService, IMapper mapper, IEmailService emailService, IOptions<AppLinkSettings> appLinkSettings)
        {
            _authRepository = authRepository;
            _tokenService = tokenService;
            _mapper = mapper;
            _emailService = emailService;
            _appLinkSettings = appLinkSettings.Value;
        }

        public async Task<ApiResponse<bool>> GenerateResetLink(string email, string userName)
        {
            var token = TokenUtils.GenerateSecureToken();
            var hashedToken = TokenUtils.HashToken(token);
            var resetUrl = $"{_appLinkSettings.PasswordResetBaseUrl}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

            EmailMessage emailMessage = new EmailMessage
            {
                ToEmail = email,
                Subject = "Password Reset Request",
                Body = TokenUtils.BuildPasswordResetHtml(userName, DateTime.UtcNow.AddHours(1), resetUrl),
                IsBodyHtml = true
            };
            var sendEmailTask = await _emailService.SendEmailAsync(emailMessage);

            if(sendEmailTask.Success)
            {
                await _authRepository.SavePasswordResetToken(email, hashedToken, DateTime.UtcNow.AddHours(1));
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Password reset link sent successfully.",
                    Data = true
                };
            }
            else
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to send password reset link.",
                    Data = false
                };
            }
        }

        public async Task<bool> IsUserExist(string email)
        {
            return await _authRepository.IsUserExist(email);
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginUser(LoginDto loginDto)
        {
            var responseFromRepo = await _authRepository.Login(loginDto);
            if (!responseFromRepo.Success || responseFromRepo.Data is null)
            {
                return new ApiResponse<LoginResponseDto>
                {
                    Success = false,
                    Message = "Invalid login credentials.",
                    Data = null,
                };
            }

            using var hmac = new HMACSHA256(responseFromRepo.Data.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
            if (computedHash.SequenceEqual(responseFromRepo.Data.PasswordHash))
            {
                var userDataForToken = _mapper.Map<UserDataForTokenGeneration>(responseFromRepo.Data);
                var userRoles = await _authRepository.GetUserRoles(responseFromRepo.Data.Id);
                var tokenResult = await _tokenService.CreateAccessTokenAsync(userDataForToken,userRoles);
                var refresh = Tokenutils.GenerateToken();
                var refreshToken = new RefreshToken
                {
                    Token = refresh.hashedToken,
                    UserId = responseFromRepo.Data.Id.ToString(),
                    Expires = tokenResult.expires.AddDays(7),
                    Created = DateTime.UtcNow,
                };
                await _authRepository.SaveRefreshToken(refreshToken);

                // Always cast the stored numeric gender value to the Gender enum for consistent downstream usage.
                var genderEnumValue = (Gender)responseFromRepo.Data.Gender;

                var userResponse = _mapper.Map<LoginResponseDto>(responseFromRepo.Data);
                userResponse.Gender = genderEnumValue.ToString(); // Convert enum to string for the response
                userResponse.AccessToken = tokenResult.token;
                userResponse.AccessTokenExpire = tokenResult.expires;
                userResponse.RefreshToken = refresh.hashedToken;
                userResponse.RefreshTokenExpire = refreshToken.Expires;
                return new ApiResponse<LoginResponseDto>
                {
                    Success = true,
                    Message = "Login successful.",
                    Data = userResponse,
                };
            }

            return new ApiResponse<LoginResponseDto>
            {
                Success = false,
                Message = "Invalid login credentials.",
                Data = null,
            };


        }

        public async Task<ApiResponse<AccessAndRefreshToken>> RefreshToken(RefreshTokenModel refreshToken)
        {
            var responseFromRepo = await _authRepository.RefreshTokenWithUser(refreshToken);

            if (!responseFromRepo.Success || responseFromRepo.Data == null)
            {
                return new ApiResponse<AccessAndRefreshToken>
                {
                    Success = false,
                    Message = "Invalid refresh token.",
                    Data = null
                };
            }

            responseFromRepo.Data.Revoked = DateTime.UtcNow;
            var userDataToGenerateToken = new UserDataForTokenGeneration
            {
                Id = responseFromRepo.Data.Id,
                FirstName = responseFromRepo.Data.FirstName,
                LastName = responseFromRepo.Data.LastName,
                Email = responseFromRepo.Data.Email,
                Phone = responseFromRepo.Data.Phone
            };
            var userRole = await _authRepository.GetUserRoles(userDataToGenerateToken.Id);
            var (newAccessToken, newAccessTokenExpiry) = await _tokenService.CreateAccessTokenAsync(userDataToGenerateToken,userRole);
            var newToken = Tokenutils.GenerateToken();
            var tokenData = new RefreshToken
            {
                Token = newToken.hashedToken,
                UserId = responseFromRepo.Data.Id.ToString(),
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(15)
            };

            await _authRepository.SaveRefreshToken(tokenData);
            var newTokens = new AccessAndRefreshToken
            {
                AccessToken = newAccessToken,
                RefreshToken = newToken.hashedToken,
                RefreshTokenExpire = tokenData.Expires,
                AccessTokenExpire = newAccessTokenExpiry
            };

            return new ApiResponse<AccessAndRefreshToken>
            {
                Success = true,
                Message = "Token refreshed successfully",
                Data = newTokens
            };
        }
        public async Task<ApiResponse<User>> RegisterUser(UserDto userDto)
        {
            if (!Enum.TryParse<Gender>(userDto.Gender, true, out var genderEnum))
            {
                return new ApiResponse<User>
                {
                    Success = false,
                    Message = "Invalid gender value"
                };
            };
            var hmac = new HMACSHA256();
            var user = new User
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Gender = (int)genderEnum,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(userDto.Password)),
                PasswordSalt = hmac.Key,
                Phone = userDto.Phone,
                ResetToken = null,
                ResetTokenExpiry = null,
                CreatedBy = userDto.FirstName + " " + userDto.LastName,
            };

            var result = await _authRepository.RegisterUser(user);
            return result;
        }

        public async Task<ApiResponseRaw> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var hmac = new HMACSHA256();
            byte[] hashedToken = Encoding.UTF8.GetBytes(TokenUtils.HashToken(resetPasswordDto.Token));
            var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(resetPasswordDto.Password));
            var saltKey = hmac.Key;

            return await _authRepository.ResetPassword(resetPasswordDto.Email, hashedToken, passwordHash, saltKey);
        }
    }
}


