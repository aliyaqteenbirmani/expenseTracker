using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DTOs.CashTransactionDtos
{
    public class CashTransactionFileDto
    {
        public string FileName { get; set; }
        public string FilePath { get; set; } = null;
    }
}
