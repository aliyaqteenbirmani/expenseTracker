using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class ConfirmReceiptDraftDto
    {
        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Amount { get; set; }

        [Required]
        [RegularExpression("^(CashIn|CashOut)$")]
        public string TransactionType { get; set; }

        [Required]
        public DateTime? TransactionDate { get; set; }

        [StringLength(500)]
        public string Merchant { get; set; }

        [StringLength(500)]
        public string Remarks { get; set; }

        [StringLength(50)]
        public string Category { get; set; }
    }
}
