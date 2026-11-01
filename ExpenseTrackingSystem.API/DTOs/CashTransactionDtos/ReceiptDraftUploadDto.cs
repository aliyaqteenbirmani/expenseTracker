using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class ReceiptDraftUploadDto
    {
        [Required]
        public Guid CashBookId { get; set; }

        [Required]
        public IFormFile File { get; set; }
    }
}
