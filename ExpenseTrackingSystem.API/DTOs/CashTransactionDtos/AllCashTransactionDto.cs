using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class AllCashTransactionDto
    {
        public Guid CashBookId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }  
        public string Remarks { get; set; }
        public string FilePath { get; set; } = null;
        public string FileName { get; set; } = string.Empty;
    }
}
