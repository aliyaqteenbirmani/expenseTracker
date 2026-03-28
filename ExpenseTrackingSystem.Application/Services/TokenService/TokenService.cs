using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SpendwiseSystem.Application.Services.TokenService
{
        public class TokenService : ITokenService
        {
            private readonly IConfiguration _configuration;
            public TokenService(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public string GenerateJwtToken(UserDataForTokenGeneration user)
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName.ToString() +" " + user.LastName.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
            };
                /*if (user.UserRoles != null && user.UserRoles.Any())
                {
                    foreach (var role in user.UserRoles)
                    {
                        if (role.Role != null)
                            claims.Add(new Claim(ClaimTypes.Role, role.Role.Name));
                    }

                }*/
                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"])),
                    signingCredentials: credentials
                    );

                return new JwtSecurityTokenHandler().WriteToken(token);

            }


        }

}


