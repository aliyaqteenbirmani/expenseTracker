using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.AuthService;
using SpendwiseSystem.Application.Services.CurrentUserService;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.DTOs.AuthDtos;
using SpendwiseSystem.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUser;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, ICurrentUserService currentUser, IConfiguration configuration)
        {
            _authService = authService;
            _currentUser = currentUser;
            _configuration = configuration;
        }

        [HttpPost("Registration")]
        public async Task<ActionResult<object>> Register(UserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponses.BadRequest("Invalid user data", "INVALID_USER_DATA"));
            }

            try
            {
                var response = await _authService.RegisterUser(userDto);

                if (!response.Success)
                {
                    return Conflict(ApiResponses.Conflict("REGISTRATION_FAILED"));
                }

                userDto.Password = null;
                return Created(string.Empty, ApiResponses.Created(response.Data, response.Message));

            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    ApiResponses.InternalServerError(ex.Message)
                );
            }
        }


        /*[HttpDelete("delete-user")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            if (email is null)
            {
                return BadRequest("User not exist.");
            }

            await _authRepository.DeleteUserAsync(email);
            return StatusCode(StatusCodes.Status204NoContent);
        }*/



        [HttpPost("Login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            var serviceResponse = await _authService.LoginUser(loginDto);

            if (!serviceResponse.Success)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            return Ok(ApiResponses.SuccessWithData(serviceResponse.Data, "Login successful"));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel refreshToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(refreshToken);

            var serviceResponse = await _authService.RefreshToken(refreshToken);
            if (serviceResponse.Success)
            {
                return Ok(new ApiResponse<AccessAndRefreshToken>
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Success = true,
                    Message = "Token refreshed successfully",
                    Data = serviceResponse.Data
                });
            }

            return Unauthorized(new ApiResponse<AccessAndRefreshToken>
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Success = false,
                Message = serviceResponse.Message,
                Data = null
            });

        }


        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([Required] string email)
        {
            if (!await IsUserExist(email))
            {
                return Accepted(new
                {
                    Success = true,
                    Message = "If the email exists, an password link has been sent."
                });
            }

            var userName = _currentUser.UserName;
            var result = await _authService.GenerateResetLink(email, userName);
            
            if(result.Success)
            {
                return Accepted(new
                {
                    Success = true,
                    Message = "If the provided email is valid, you will receive instructions to reset your password."
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to send password reset link."
                });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if ((!ModelState.IsValid))
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ResetPassword(resetPasswordDto);
            if(result.Success)
                return Ok(new { message = "Password reset successfully. You can now log in." });

            return BadRequest(new { message = result.Message });
        }



        private async Task<bool> IsUserExist(string email)
        {
            return await _authService.IsUserExist(email);
        }
    }

}



