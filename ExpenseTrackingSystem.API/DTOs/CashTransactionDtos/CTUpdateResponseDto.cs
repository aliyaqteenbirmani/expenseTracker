using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class CTUpdateResponseDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }
        public string Remarks { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OldFileName { get; set; } = string.Empty;
    }
}
