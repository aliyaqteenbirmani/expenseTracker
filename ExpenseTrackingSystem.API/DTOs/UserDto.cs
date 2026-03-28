using SpendwiseSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SpendwiseSystem.Domain.DTOs
{
    public class UserDto
    {
        [Required]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Gender Gender { get; set; }
        [Required]
        public string Phone { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }

    }

    public class UserRegisterResponseDto
    {
        public int StatusCode { get; set; }
        public string ResponseMessage { get; set; }
    }
}


