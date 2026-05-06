using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DTOs.CashBookDtos
{
    public class CashBookMemberDto
    {
        public Guid CashbookMemberId { get; set; }
        public Guid CashBookId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? AddedOn { get; set; }
        public DateTime InvitedOn { get; set; }
        public List<string> Permissions { get; set; } = new();
    }
}
