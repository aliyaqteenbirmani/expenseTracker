using System.ComponentModel.DataAnnotations;

namespace ExpenseTrackingSystem.Domain.DTOs
{
    public class UpdateCashBookDto
    {
        [Required]
        [MaxLength(200)]
        public string BookName { get; set; }
    }
}
