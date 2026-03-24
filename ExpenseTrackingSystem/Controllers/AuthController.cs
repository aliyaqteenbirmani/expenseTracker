using ExpenseTrackingSystem.Application.Expressions;
using ExpenseTrackingSystem.Application.Interfaces;
using ExpenseTrackingSystem.Application.Services.AuthService;
using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace ExpenseTrackingSystem.API.Controllers
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
        public async Task<ActionResult> Register(UserDto userDto)
        {
            try
            {
                var response = await _authService.RegisterUser(userDto);

                if(!response)
                {
                    return BadRequest(new UserApiResponse<UserDto>
                    {
                        Success = false,
                        Message = " Registration failed",
                        Data = null
                    });
                }
                return Ok(new UserApiResponse<UserDto>
                {
                    Success = true,
                    Message = " Registration successful",
                    Data = userDto
                });
            }
            catch(SqlException ex) when (ex.Number == 50001)
            {
                return Conflict(new UserApiResponse<UserDto>
                {
                    Success = false,
                    Message = " Email already registered",
                    Data = null
                });
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
        public async Task<ActionResult<UserApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            var serviceResponse = await _authService.LoginUser(loginDto);

            if (!serviceResponse.Success)
            {
                return BadRequest(new UserApiResponse<User>
                {
                    Success = false,
                    Message = serviceResponse.Message,
                    Data = null
                });
            }
                //return NotFound(new { message = "Invalid UserName or Password" });

            return Ok(new UserApiResponse<LoginResponseDto>
            {
                Success = true,
                Message = "Login successful",
                Data = serviceResponse.Data,
                Token = serviceResponse.Token
            });
        }


        /*[HttpPost("assign")]
        public async Task<IActionResult> AssignRoleToUser(Guid userId, string roleName)
        {
            try
            {
                await _roleService.AssignRoleToUser(userId, roleName);
                return Ok(new { message = "Role assigned successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpPost("forgot-password")]
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

