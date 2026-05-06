using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DTOs.InvitationRequestDto
{
    public class CreateBusinessInvitationRequest
    {
        public string InvitedEmail { get; set; } = string.Empty;
        public List<int> Permissions { get; set; } = new();
        public DateTime? ExpiresOn { get; set; }
        public string? Remarks { get; set; }
    }

    public class CreateCashbookInvitationRequest
    {
        public string InvitedEmail { get; set; } = string.Empty;
        public List<int> Permissions { get; set; } = new();
        public DateTime? ExpiresOn { get; set; }
        public string? Remarks { get; set; }
    }

    public class RejectInvitationRequest
    {
        public string? Remarks { get; set; }
    }

    public class UpdateBusinessMemberPermissionsRequest
    {
        public List<int> Permissions { get; set; } = new();
    }

    public class UpdateCashbookMemberPermissionsRequest
    {
        public List<int> Permissions { get; set; } = new();
    }
}
