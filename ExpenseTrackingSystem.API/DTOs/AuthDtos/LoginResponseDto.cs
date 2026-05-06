using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Domain.Enums;

namespace SpendwiseSystem.Domain.DTOs.AuthDtos
{
    public class LoginResponseDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpire { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpire { get; set; }
        //public string UserId { get; set; }


    }
}


