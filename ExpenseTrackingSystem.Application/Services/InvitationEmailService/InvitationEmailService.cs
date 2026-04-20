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

        public async Task<ApiResponse<bool>> SendBusinessInvitationEmailAsync(string toEmail, string invitedByName, string businessName, List<string> permissions, DateTime? expiresOn, string remarks, string invitationLink)
        {
            var subject = $"Invitation to join {businessName} on Spendwise";
            var body = BuildBusinessInvitationHtml(
                invitedByName,
                businessName,
                permissions,
                expiresOn,
                remarks,
                invitationLink);

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

        public async Task<ApiResponse<bool>> SendCashbookInvitationEmailAsync(string toEmail, string invitedByName, string businessName, string cashbookName, List<string> permissions, DateTime? expiresOn, string remarks, string invitationLink)
        {
            var subject = $"Invitation to join cashbook: {cashbookName}";
            var body = BuildCashbookInvitationHtml(
                invitedByName,
                businessName,
                cashbookName,
                permissions,
                expiresOn,
                remarks,
                invitationLink);

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
            string? remarks,
            string invitationLink)
        {
            var formattedPermissions = permissions?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(FormatPermission)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            var permissionsHtml = formattedPermissions.Any()
                ? string.Join("", formattedPermissions.Select(p =>
                    $"<li style='margin: 0 0 8px 0; color: #374151;'>{p}</li>"))
                : "<li style='color: #6b7280;'>No permissions assigned</li>";

            var expirySection = expiresOn.HasValue
                ? $@"
            <tr>
                <td style='padding: 0 0 16px 0; font-size: 14px; color: #374151;'>
                    <strong>Invitation Expiry:</strong> {expiresOn.Value:dd MMM yyyy hh:mm tt}
                </td>
            </tr>"
                : string.Empty;

            var remarksSection = !string.IsNullOrWhiteSpace(remarks)
                ? $@"
            <tr>
                <td style='padding: 0 0 16px 0; font-size: 14px; color: #374151;'>
                    <strong>Remarks:</strong> {System.Net.WebUtility.HtmlEncode(remarks)}
                </td>
            </tr>"
                : string.Empty;

            return $@"
                    <!DOCTYPE html>
                    <html lang='en'>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Business Invitation</title>
                    </head>
                    <body style='margin:0; padding:0; background-color:#f3f4f6; font-family:Arial, Helvetica, sans-serif;'>
                        <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color:#f3f4f6; margin:0; padding:24px 0;'>
                            <tr>
                                <td align='center'>
                                    <table role='presentation' width='640' cellspacing='0' cellpadding='0' border='0' style='width:640px; max-width:640px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 8px 24px rgba(0,0,0,0.08);'>
                                        
                                        <tr>
                                            <td style='background:linear-gradient(135deg, #2563eb, #1d4ed8); padding:24px 32px;'>
                                                <div style='font-size:24px; font-weight:700; color:#ffffff;'>Spendwise</div>
                                                <div style='font-size:13px; color:#dbeafe; margin-top:4px;'>Business Collaboration Invitation</div>
                                            </td>
                                        </tr>
                    
                                        <tr>
                                            <td style='padding:32px;'>
                                                <h2 style='margin:0 0 16px 0; font-size:28px; line-height:1.3; color:#111827;'>
                                                    You have been invited to join a business
                                                </h2>
                    
                                                <p style='margin:0 0 20px 0; font-size:15px; line-height:1.8; color:#374151;'>
                                                    <strong>{System.Net.WebUtility.HtmlEncode(invitedByName)}</strong>
                                                    has invited you to join the business
                                                    <strong>{System.Net.WebUtility.HtmlEncode(businessName)}</strong>.
                                                </p>
                    
                                                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='margin-bottom:24px; background-color:#f9fafb; border:1px solid #e5e7eb; border-radius:12px;'>
                                                    <tr>
                                                        <td style='padding:20px;'>
                                                            <div style='font-size:15px; font-weight:700; color:#111827; margin-bottom:12px;'>
                                                                Assigned Permissions
                                                            </div>
                                                            <ul style='padding-left:20px; margin:0;'>
                                                                {permissionsHtml}
                                                            </ul>
                                                        </td>
                                                    </tr>
                                                </table>
                    
                                                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'>
                                                    {expirySection}
                                                    {remarksSection}
                                                </table>
                    
                                                <p style='margin:0 0 24px 0; font-size:14px; line-height:1.7; color:#4b5563;'>
                                                    Click the button below to view this invitation in Spendwise.
                                                </p>
                    
                                                <table role='presentation' cellspacing='0' cellpadding='0' border='0' style='margin-bottom:24px;'>
                                                    <tr>
                                                        <td align='center' style='border-radius:10px; background-color:#16a34a;'>
                                                            <a href='{invitationLink}'
                                                               style='display:inline-block; padding:14px 28px; font-size:15px; font-weight:700; color:#ffffff; text-decoration:none; border-radius:10px;'>
                                                                Open Invitation
                                                            </a>
                                                        </td>
                                                    </tr>
                                                </table>
                    
                                                <p style='margin:0 0 8px 0; font-size:13px; color:#6b7280;'>
                                                    If the button does not work, copy and paste this link into your browser:
                                                </p>
                    
                                                <p style='margin:0 0 24px 0; font-size:13px; line-height:1.7; word-break:break-all;'>
                                                    <a href='{invitationLink}' style='color:#2563eb; text-decoration:none;'>{invitationLink}</a>
                                                </p>
                    
                                                <p style='margin:0; font-size:14px; line-height:1.7; color:#4b5563;'>
                                                    Thank you,<br/>
                                                    <strong>Spendwise Team</strong>
                                                </p>
                                            </td>
                                        </tr>
                    
                                        <tr>
                                            <td style='padding:18px 32px; background-color:#f9fafb; border-top:1px solid #e5e7eb;'>
                                                <p style='margin:0; font-size:12px; line-height:1.6; color:#6b7280;'>
                                                    This invitation was sent by Spendwise. If you were not expecting this email, you can safely ignore it.
                                                </p>
                                            </td>
                                        </tr>
                    
                                    </table>
                                </td>
                            </tr>
                        </table>
                    </body>
                    </html>";
        }

        private static string BuildCashbookInvitationHtml(
            string invitedByName,
            string businessName,
            string cashbookName,
            List<string> permissions,
            DateTime? expiresOn,
            string? remarks,
            string invitationLink)
        {
            var formattedPermissions = permissions?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(FormatPermission)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            var permissionsHtml = formattedPermissions.Any()
                ? string.Join("", formattedPermissions.Select(p =>
                    $"<li style='margin: 0 0 8px 0; color: #374151;'>{p}</li>"))
                : "<li style='color: #6b7280;'>No permissions assigned</li>";

            var expirySection = expiresOn.HasValue
                ? $@"
            <tr>
                <td style='padding: 0 0 16px 0; font-size: 14px; color: #374151;'>
                    <strong>Invitation Expiry:</strong> {expiresOn.Value:dd MMM yyyy hh:mm tt}
                </td>
            </tr>"
                : string.Empty;

            var remarksSection = !string.IsNullOrWhiteSpace(remarks)
                ? $@"
            <tr>
                <td style='padding: 0 0 16px 0; font-size: 14px; color: #374151;'>
                    <strong>Remarks:</strong> {System.Net.WebUtility.HtmlEncode(remarks)}
                </td>
            </tr>"
                : string.Empty;

            return $@"
                    <!DOCTYPE html>
                    <html lang='en'>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Cashbook Invitation</title>
                    </head>
                    <body style='margin:0; padding:0; background-color:#f3f4f6; font-family:Arial, Helvetica, sans-serif;'>
                        <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color:#f3f4f6; margin:0; padding:24px 0;'>
                            <tr>
                                <td align='center'>
                                    <table role='presentation' width='640' cellspacing='0' cellpadding='0' border='0' style='width:640px; max-width:640px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 8px 24px rgba(0,0,0,0.08);'>
                                        
                                        <tr>
                                            <td style='background:linear-gradient(135deg, #2563eb, #1d4ed8); padding:24px 32px;'>
                                                <div style='font-size:24px; font-weight:700; color:#ffffff;'>Spendwise</div>
                                                <div style='font-size:13px; color:#dbeafe; margin-top:4px;'>Business Collaboration Invitation</div>
                                            </td>
                                        </tr>
                    
                                        <tr>
                                            <td style='padding:32px;'>
                                                <h2 style='margin:0 0 16px 0; font-size:28px; line-height:1.3; color:#111827;'>
                                                    You have been invited to join a cashbook
                                                </h2>
                    
                                                <p style='margin:0 0 20px 0; font-size:15px; line-height:1.8; color:#374151;'>
                                                    <strong>{System.Net.WebUtility.HtmlEncode(invitedByName)}</strong>
                                                    has invited you to join the cashbook
                                                    <strong>{System.Net.WebUtility.HtmlEncode(cashbookName)}</strong>.
                                                </p>
                    
                                                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='margin-bottom:24px; background-color:#f9fafb; border:1px solid #e5e7eb; border-radius:12px;'>
                                                    <tr>
                                                        <td style='padding:20px;'>
                                                            <div style='font-size:15px; font-weight:700; color:#111827; margin-bottom:12px;'>
                                                                Assigned Permissions
                                                            </div>
                                                            <ul style='padding-left:20px; margin:0;'>
                                                                {permissionsHtml}
                                                            </ul>
                                                        </td>
                                                    </tr>
                                                </table>
                    
                                                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'>
                                                    {expirySection}
                                                    {remarksSection}
                                                </table>
                    
                                                <p style='margin:0 0 24px 0; font-size:14px; line-height:1.7; color:#4b5563;'>
                                                    Click the button below to view this invitation in Spendwise.
                                                </p>
                    
                                                <table role='presentation' cellspacing='0' cellpadding='0' border='0' style='margin-bottom:24px;'>
                                                    <tr>
                                                        <td align='center' style='border-radius:10px; background-color:#16a34a;'>
                                                            <a href='{invitationLink}'
                                                               style='display:inline-block; padding:14px 28px; font-size:15px; font-weight:700; color:#ffffff; text-decoration:none; border-radius:10px;'>
                                                                Open Invitation
                                                            </a>
                                                        </td>
                                                    </tr>
                                                </table>
                    
                                                <p style='margin:0 0 8px 0; font-size:13px; color:#6b7280;'>
                                                    If the button does not work, copy and paste this link into your browser:
                                                </p>
                    
                                                <p style='margin:0 0 24px 0; font-size:13px; line-height:1.7; word-break:break-all;'>
                                                    <a href='{invitationLink}' style='color:#2563eb; text-decoration:none;'>{invitationLink}</a>
                                                </p>
                    
                                                <p style='margin:0; font-size:14px; line-height:1.7; color:#4b5563;'>
                                                    Thank you,<br/>
                                                    <strong>Spendwise Team</strong>
                                                </p>
                                            </td>
                                        </tr>
                    
                                        <tr>
                                            <td style='padding:18px 32px; background-color:#f9fafb; border-top:1px solid #e5e7eb;'>
                                                <p style='margin:0; font-size:12px; line-height:1.6; color:#6b7280;'>
                                                    This invitation was sent by Spendwise. If you were not expecting this email, you can safely ignore it.
                                                </p>
                                            </td>
                                        </tr>
                    
                                    </table>
                                </td>
                            </tr>
                        </table>
                    </body>
                    </html>";
        }

        private static string FormatPermission(string permission)
        {
            return permission?.Trim().ToUpperInvariant() switch
            {
                "BUSINESS_VIEW" => "View business",
                "CASHBOOK_LIST_VIEW" => "View cashbook list",
                "CASHBOOK_CREATE" => "Create cashbook",
                "CASHBOOK_UPDATE" => "Update cashbook",
                "CASHBOOK_DELETE" => "Delete cashbook",
                "CASHBOOK_VIEW" => "View cashbook",
                "TRANSACTION_VIEW" => "View transactions",
                "TRANSACTION_ADD" => "Add transactions",
                "TRANSACTION_EDIT" => "Edit transactions",
                "TRANSACTION_DELETE" => "Delete transactions",
                _ => permission ?? string.Empty
            };
        }
    }
    
}
