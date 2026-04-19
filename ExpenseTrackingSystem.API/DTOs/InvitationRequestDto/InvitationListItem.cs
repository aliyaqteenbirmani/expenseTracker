using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DTOs.InvitationRequestDto
{
    public class InvitationListItem
    {
        public Guid Id { get; set; }
        public string InvitationType { get; set; } = string.Empty;
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public Guid? CashBookId { get; set; }
        public string? CashbookName { get; set; }
        public Guid? InvitedUserId { get; set; }
        public string InvitedEmail { get; set; } = string.Empty;
        public Guid InvitedByUserId { get; set; }
        public string InvitedByName { get; set; } = string.Empty;
        public string InvitedByEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresOn { get; set; }
        public DateTime? RespondedOn { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedOn { get; set; }

        public List<string> Permissions { get; set; } = new();
    }
}
