using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.DTOs.InvitationRequestDto
{
    public class BusinessEmailInfoDto
    {
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
    }

    public class CashbookEmailInfoDto
    {
        public Guid CashBookId { get; set; }
        public Guid BusinessId { get; set; }
        public string CashbookName { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
    }

    public class UserEmailInfoDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
