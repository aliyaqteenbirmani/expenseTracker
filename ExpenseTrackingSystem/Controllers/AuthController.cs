using SpendwiseSystem.Application.Expressions;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.AuthService;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("Registration")]
        public async Task<ActionResult<ApiResponse<object>>> Register(UserDto userDto)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ApiResponses.BadRequest("Invalid user data", "INVALID_USER_DATA"));
            }

            try
            {
                var response = await _authService.RegisterUser(userDto);

                if (!response.Success)
                {
                    return Conflict(
                        new ApiResponse<object>
                        {
                            StatusCode = (int)HttpStatusCode.Conflict,
                            Success = false,
                            Message = response.Message,
                            ErrorCode = "REGISTRATION_FAILED",
                            Data = null
                        }
                    );
                }
                userDto.Password = null;
                return CreatedAtAction(nameof(Register),
                    new ApiResponse<object>
                    {
                        StatusCode = (int)HttpStatusCode.Created,
                        Success = true,
                        Message = response.Message,
                        ErrorCode = "REGISTRATION_SUCCESS",
                        Data = response
                    }
                );
            }
            catch (SqlException ex) when (ex.Number == 50001) 
            {
                return Conflict(ApiResponses.Conflict("Unable to process request. Please try again or use a different email."));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    ApiResponses.InternalServerError("An unexpected error occurred. Please try again later.")
                );
            }
        }
        /*public async Task<ActionResult> Register(UserDto userDto)
        //{
        //    try
        //    {
        //        var response = await _authService.RegisterUser(userDto);

        //        if(!response.Success)
        //        {
        //            return BadRequest(new ApiResponse<UserRegisterResponseDto>
        //            {
        //                StatusCode = 200,
        //                Success = true,
        //                Message = "Success",
        //                Data = new UserRegisterResponseDto { ResponseMessage= "Unable to process request. Please try again or use different email.", StatusCode=409 }
        //            });
        //        }
        //        return Ok(new ApiResponse<UserDto>
        //        {
        //            StatusCode = 201,
        //            Success = true,
        //            Message = " Registration successful",
        //            Data = userDto
        //        });
        //    }
        //    catch(SqlException ex) when (ex.Number == 50001)
        //    {
        //        return Conflict(new ApiResponse<UserDto>
        //        {
        //            StatusCode = 409,
        //            Success = false,
        //            Message = ex.Message+" Email already registered",
        //            Data = null
        //        });
        //    }
        //}*/


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
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            var serviceResponse = await _authService.LoginUser(loginDto);

            if (!serviceResponse.Success)
            {
                return Unauthorized(new ApiResponse<LoginResponseDto>
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Success = false,
                    Message = serviceResponse.Message,
                    Data = null
                });
            }

            return Ok(new ApiResponse<LoginResponseDto>
            {
                StatusCode = (int)HttpStatusCode.OK,
                Success = true,
                Message = "Login successful",
                Data = serviceResponse.Data,
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel refreshToken)
        {
            if(!ModelState.IsValid)
                return BadRequest(refreshToken);

            var serviceResponse = await _authService.RefreshToken(refreshToken);
            if(serviceResponse.Success)
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


        /*[HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([Required] ForgotPasswordDto email)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(email);
            }

            var user = await _authRepository.GetUserByEmail(email.Email);
            if (user is null)
            {
                return BadRequest(new { message = "Email doesn't exist." });
            }

            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            _foodAppContext.Users.Update(user);
            await _foodAppContext.SaveChangesAsync();

            var resetUrl = $"https://localhost:7037/api/Authentication/reset-password" +
                $"?token={Uri.EscapeDataString(user.ResetToken)}" +
                $"&email={Uri.EscapeDataString(user.Email)}";

            await SendEmailAsync(user.Email, "Reset Your Password",
                $"Hey there! Click this link to reset password: <a href='{resetUrl}'>{resetUrl}</a>");

            return Ok(new { message = " If that email is registered, a reset link has been sent." });
        }


        //[HttpPost("reset-password")]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        //{
        //    if ((!ModelState.IsValid))
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var user = await _foodAppContext.Users.FirstOrDefaultAsync(u => u.Email == resetPasswordDto.Email && 
        //    u.Email == resetPasswordDto.Email &&
        //    u.ResetToken == resetPasswordDto.Token &&
        //    u.ResetTokenExpiry > DateTime.UtcNow);

        //    if (user is null)
        //    {
        //        return BadRequest(new { Message = "Invalid or expired reset token." });
        //    }

        //    var hmac = new HMACSHA256();
        //    user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(resetPasswordDto.Password));
        //    user.PasswordSalt = hmac.Key;
        //    user.ResetToken = null;
        //    user.ResetTokenExpiry = null;
        //    await _foodAppContext.SaveChangesAsync();

        //    return Ok(new { message = "Password reset successfully. You can now log in." });
        //}

        [HttpGet("reset-password")]
        public async Task<IActionResult> ResetPasswordAuto(
        [FromQuery] string token,
        [FromQuery] string email)
        {
            var user = await _foodAppContext.Users.FirstOrDefaultAsync(u =>
                u.Email == email &&
                u.ResetToken == token &&
                u.ResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                return BadRequest(new { Message = "Invalid or expired reset token." });
            }

            // Generate a temporary password
            var tempPassword = GenerateTempPassword();
            var hmac = new HMACSHA256();
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(tempPassword));
            user.PasswordSalt = hmac.Key;
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _foodAppContext.SaveChangesAsync();

            // Send the temp password to the user
            await SendEmailAsync(user.Email, "Your New Temporary Password",
                $"Your temporary password is: {tempPassword}. Log in and change it soon!");

            return Ok(new { Message = "Password reset successfully. Check your email for your temporary password." });
        }

        private string GenerateTempPassword()
        {
            // Simple temp password generator (8 chars, letters + numbers)
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portValue = _configuration["EmailSettings:Port"];
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];

            if (string.IsNullOrEmpty(portValue) || !int.TryParse(portValue, out int port))
            {
                throw new InvalidOperationException("SMTP port is missing or invalid in configuration.");
            }

            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("SMTP configuration is incomplete.");
            }

            using var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new System.Net.NetworkCredential(username, password),
                EnableSsl = true // This enables TLS
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(username),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage); // Line 254 - Error here
        }*/

    }

}



