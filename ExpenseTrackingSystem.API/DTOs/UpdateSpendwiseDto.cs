using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs
{
    public class UpdateSpendwiseDto
    {
        [Required]
        [MaxLength(200)]
        public string SpendwiseName { get; set; }
    }
}




