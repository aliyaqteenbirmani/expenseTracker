using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs
{
    public class CreateCashBookDto
    {
        [Required]
        [MaxLength(200)]
        public string CashBookName { get; set; }
        [Required]
        public Guid BusinessId { get; set; }
    }
}




