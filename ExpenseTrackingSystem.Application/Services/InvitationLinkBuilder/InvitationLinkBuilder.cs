using SpendwiseSystem.Domain.Entities.ConfigModel;
using System;

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
            var rawUrl = _appLinkSettings?.InvitationBaseUrl?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawUrl) || rawUrl.Contains("sameyahusa-web.web.app", StringComparison.OrdinalIgnoreCase))
            {
                rawUrl = "http://localhost:5173";
            }

            var baseUrl = rawUrl.TrimEnd('/', '?', '&');
            if (baseUrl.EndsWith("/invite/accept", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - "/invite/accept".Length).TrimEnd('/');
                if (string.IsNullOrWhiteSpace(baseUrl))
                    baseUrl = "http://localhost:5173";
            }

            var separator = baseUrl.Contains('?') ? "&" : "?";
            return $"{baseUrl}{separator}inviteToken={Uri.EscapeDataString(inviteToken)}";
        }
    }
}
