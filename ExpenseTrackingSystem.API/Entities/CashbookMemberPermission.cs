using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.Entities
{
    public class CashbookMemberPermission : BaseEntity
    {
        public Guid CashbookMemberId { get; set; }
        public Guid PermissionId { get; set; }
        public bool Granted { get; set; }
    }
}
