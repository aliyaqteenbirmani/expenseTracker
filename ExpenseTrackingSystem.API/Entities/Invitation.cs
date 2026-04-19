using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.Entities
{
    public class Invitation : BaseEntity
    {
        public string InvitationType { get; set; } = string.Empty;
        public Guid BusinessId { get; set; }
        public Guid? CashBookId { get; set; }
        public Guid InvitedUserId { get; set; }
        public Guid InvitedByUserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresOn { get; set; }
        public DateTime? RespondedOn { get; set; }
        public string? Remarks { get; set; }

        public List<InvitationPermission> Permissions { get; set; } = new();
    }
}
