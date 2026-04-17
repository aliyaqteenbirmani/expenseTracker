using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.Entities
{
    public class CashTransaction
    {
        public Guid Id { get; set; }
        public Guid CashBookId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }   
        public string Remarks { get; set; }
        public string FileName { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
