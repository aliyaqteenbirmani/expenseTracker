using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Domain.Entities
{
    public class InvitationPermission : BaseEntity
    {
        public Guid InvitationId { get; set; }
        public Guid PermissionId { get; set; }
        public bool Granted { get; set; }
    }
}
