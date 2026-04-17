using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class CashTransactionDto
    {
        public Guid CashBookId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Amount must be a positive value.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "TransactionType is required.")]
        [StringLength(20, ErrorMessage = "TransactionType must not exceed 20 characters.")]
        [RegularExpression(@"^(CashIn|CashOut)$", ErrorMessage = "TransactionType must be 'CashIn' or 'CashOut'.")]
        public string TransactionType { get; set; }   // CashIn / CashOut

        [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
        [DataType(DataType.MultilineText)]
        public string Remarks { get; set; }
        public IFormFile File { get; set; } = null;
        public string FileName { get; set; } = string.Empty;
    }
}
