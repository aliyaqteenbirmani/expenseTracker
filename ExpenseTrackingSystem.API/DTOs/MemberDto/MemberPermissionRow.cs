using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DTOs.MemberDto
{
    public class MemberPermissionRow
    {
        public Guid MemberId { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    public class UpdateBusinessMemberPermissionsRequest
    {
        public List<string> Permissions { get; set; } = new();
    }

    public class UpdateCashbookMemberPermissionsRequest
    {
        public List<string> Permissions { get; set; } = new();
    }
}
