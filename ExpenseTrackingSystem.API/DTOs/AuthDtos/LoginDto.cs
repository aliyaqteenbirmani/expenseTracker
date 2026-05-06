using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs.AuthDtos
{
    public class LoginDto
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}


