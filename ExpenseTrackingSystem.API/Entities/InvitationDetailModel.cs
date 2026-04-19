using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.Entities
{
    public class InvitationDetailModel
    {
        public Guid Id { get; set; }
        public string InvitationType { get; set; } = string.Empty;
        public Guid BusinessId { get; set; }
        public Guid? CashBookId { get; set; }
        public Guid InvitedUserId { get; set; }
        public string InvitedUserName { get; set; } = string.Empty;
        public string InvitedUserEmail { get; set; } = string.Empty;
        public Guid InvitedByUserId { get; set; }
        public string InvitedByUserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresOn { get; set; }
        public DateTime? RespondedOn { get; set; }
        public string? Remarks { get; set; }
        public List<string> Permissions { get; set; } = new();
        public DateTime CreatedOn { get; set; }
    }
}
