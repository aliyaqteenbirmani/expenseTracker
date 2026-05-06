using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs.CashBookDtos
{
    public class CreateCashBookDto
    {
        [Required]
        [MaxLength(200)]
        public string CashBookName { get; set; }
        [Required]
        public string BusinessId { get; set; }
    }
}




