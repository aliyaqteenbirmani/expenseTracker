using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.InvitationLinkBuilder
{
    public interface IInvitationLinkBuilder
    {
        string BuildInvitationLink(string inviteToken);
    }
}
