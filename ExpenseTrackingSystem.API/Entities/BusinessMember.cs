using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.Entities
{
    public class BusinessMember : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public Guid? InvitationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? JoinedOn { get; set; }
        public DateTime InvitedOn { get; set; }
        public Guid InvitedByUserId { get; set; }
        public DateTime? RespondedOn { get; set; }
        public string? ResponseRemarks { get; set; }

        public List<BusinessMemberPermission> Permissions { get; set; } = new();
    }
}
