using SpendwiseSystem.Application.Services.EmailService;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.InvitationEmailService
{
    public class InvitationEmailService : IInvitationEmailService
    {
        private readonly IEmailService _emailService;

        public InvitationEmailService(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<ApiResponse<bool>> SendBusinessInvitationEmailAsync(string toEmail, string invitedByName, string businessName, List<string> permissions, DateTime? expiresOn, string remarks)
        {
            var subject = $"Invitation to join {businessName} on Spendwise";
            var body = BuildBusinessInvitationHtml(
                invitedByName,
                businessName,
                permissions,
                expiresOn,
                remarks);

            var response = await _emailService.SendEmailAsync(new EmailMessage
            {
                ToEmail = toEmail,
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            });
            return new ApiResponse<bool>
            {
                StatusCode = response.Success ? 200 : 500,
                Success = response.Success,
                Message = response.Message,
                Data = response.Success
            };
        }

        public async Task<ApiResponse<bool>> SendCashbookInvitationEmailAsync(string toEmail, string invitedByName, string businessName, string cashbookName, List<string> permissions, DateTime? expiresOn, string remarks)
        {
            var subject = $"Invitation to join cashbook: {cashbookName}";
            var body = BuildCashbookInvitationHtml(
                invitedByName,
                businessName,
                cashbookName,
                permissions,
                expiresOn,
                remarks);

            return await _emailService.SendEmailAsync(new EmailMessage
            {
                ToEmail = toEmail,
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            });
        }

        private static string BuildBusinessInvitationHtml(
            string invitedByName,
            string businessName,
            List<string> permissions,
            DateTime? expiresOn,
            string? remarks)
        {
            var sb = new StringBuilder();

            sb.Append($@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <h2>You have been invited to join a business</h2>
                    <p><strong>{invitedByName}</strong> has invited you to join the business <strong>{businessName}</strong>.</p>
                    <p><strong>Assigned Permissions:</strong></p>
                    <ul>");

            foreach (var permission in permissions)
            {
                sb.Append($"<li>{permission}</li>");
            }

            sb.Append("</ul>");

            if (expiresOn.HasValue)
            {
                sb.Append($"<p><strong>Invitation Expiry:</strong> {expiresOn.Value:dd MMM yyyy hh:mm tt}</p>");
            }

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                sb.Append($"<p><strong>Remarks:</strong> {remarks}</p>");
            }

            sb.Append(@"
                    <p>Please log in to the Spendwise app to accept or reject this invitation.</p>
                    <p>Thank you,<br/>Spendwise Team</p>
                </div>");

            return sb.ToString();
        }

        private static string BuildCashbookInvitationHtml(
            string invitedByName,
            string businessName,
            string cashbookName,
            List<string> permissions,
            DateTime? expiresOn,
            string? remarks)
        {
            var sb = new StringBuilder();

            sb.Append($@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <h2>You have been invited to join a cashbook</h2>
                    <p><strong>{invitedByName}</strong> has invited you to join the cashbook <strong>{cashbookName}</strong> in business <strong>{businessName}</strong>.</p>
                    <p><strong>Assigned Permissions:</strong></p>
                    <ul>");

            foreach (var permission in permissions)
            {
                sb.Append($"<li>{permission}</li>");
            }

            sb.Append("</ul>");

            if (expiresOn.HasValue)
            {
                sb.Append($"<p><strong>Invitation Expiry:</strong> {expiresOn.Value:dd MMM yyyy hh:mm tt}</p>");
            }

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                sb.Append($"<p><strong>Remarks:</strong> {remarks}</p>");
            }

            sb.Append(@"
                    <p>Please log in to the Spendwise app to accept or reject this invitation.</p>
                    <p>Thank you,<br/>Spendwise Team</p>
                </div>");

            return sb.ToString();
        }
    }
    
}
