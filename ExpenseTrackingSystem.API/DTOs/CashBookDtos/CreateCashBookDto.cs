using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs.CashBookDtos
{
    public class CreateCashBookDto
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string CashBookName { get; set; }
        [Required]
        public string BusinessId { get; set; }
    }
}




