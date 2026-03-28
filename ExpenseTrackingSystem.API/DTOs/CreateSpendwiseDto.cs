using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs
{
    public class CreateSpendwiseDto
    {
        [Required]
        [MaxLength(200)]
        public string SpendwiseName { get; set; }
    }
}




