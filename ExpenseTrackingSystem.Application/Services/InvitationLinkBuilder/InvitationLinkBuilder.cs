using SpendwiseSystem.Domain.Entities.ConfigModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.InvitationLinkBuilder
{
    public class InvitationLinkBuilder : IInvitationLinkBuilder
    {
        private readonly AppLinkSettings _appLinkSettings;

        public InvitationLinkBuilder(AppLinkSettings appLinkSettings)
        {
            _appLinkSettings = appLinkSettings;
        }

        public string BuildInvitationLink(string inviteToken)
        {
            return $"{_appLinkSettings.InvitationBaseUrl}?token={Uri.EscapeDataString(inviteToken)}";
        }
    }
}
