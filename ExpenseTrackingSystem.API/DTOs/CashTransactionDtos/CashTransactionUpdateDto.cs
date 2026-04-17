using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class CashTransactionUpdateDto
    {
        [Required]
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }   
        public string Remarks { get; set; }
        public IFormFile File { get; set; } = null;
        public string FileName { get; set; } = null;

    }
}
