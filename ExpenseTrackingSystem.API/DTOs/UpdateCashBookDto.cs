using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs
{
    public class UpdateCashBookDto
    {
        [Required]
        [MaxLength(200)]
        public string CashBook { get; set; }
    }
}




