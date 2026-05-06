using System;
using System.Security.Cryptography;
using System.Text;

namespace SpendwiseSystem.Application.Helper
{
    public static class TokenUtils
    {
        private const int DefaultTokenSizeBytes = 62;
        public static string GenerateSecureToken(int sizeBytes = DefaultTokenSizeBytes)
        {
            if (sizeBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Token size must be greater than zero.");
            }

            var bytes = RandomNumberGenerator.GetBytes(sizeBytes);
            return ToUrlSafeBase64(bytes);
        }

        public static string HashToken(string token)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token must not be empty or whitespace.", nameof(token));
            }

            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = SHA256.HashData(tokenBytes);

            return ToUrlSafeBase64(hashBytes);
        }

        private static string ToUrlSafeBase64(byte[] bytes)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var base64 = Convert.ToBase64String(bytes);
            return base64.Replace('+', '-')
                         .Replace('/', '_')
                         .TrimEnd('=');
        }

        public static string BuildPasswordResetHtml(
            string userName,
            DateTime? expiresOn,
            string resetPasswordLink)
        {
            var expirySection = expiresOn.HasValue
                ? $@"
        <tr>
            <td style='padding: 0 0 16px 0; font-size: 14px; color: #374151;'>
                <strong>Link Expiry:</strong> {expiresOn.Value:dd MMM yyyy hh:mm tt}
            </td>
        </tr>"
                : string.Empty;

            return $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Password Reset</title>
                </head>
                <body style='margin:0; padding:0; background-color:#f3f4f6; font-family:Arial, Helvetica, sans-serif;'>
                    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color:#f3f4f6; margin:0; padding:24px 0;'>
                        <tr>
                            <td align='center'>
                                <table role='presentation' width='640' cellspacing='0' cellpadding='0' border='0' style='width:640px; max-width:640px; background-color:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 8px 24px rgba(0,0,0,0.08);'>
                                    
                                    <tr>
                                        <td style='background:linear-gradient(135deg, #2563eb, #1d4ed8); padding:24px 32px;'>
                                            <div style='font-size:24px; font-weight:700; color:#ffffff;'>Spendwise</div>
                                            <div style='font-size:13px; color:#dbeafe; margin-top:4px;'>Password Reset Request</div>
                                        </td>
                                    </tr>
                
                                    <tr>
                                        <td style='padding:32px;'>
                                            <h2 style='margin:0 0 16px 0; font-size:28px; line-height:1.3; color:#111827;'>
                                                Reset your password
                                            </h2>
                
                                            <p style='margin:0 0 20px 0; font-size:15px; line-height:1.8; color:#374151;'>
                                                Hello <strong>{System.Net.WebUtility.HtmlEncode(userName)}</strong>,
                                                we received a request to reset your Spendwise account password.
                                            </p>
                
                                            <p style='margin:0 0 24px 0; font-size:14px; line-height:1.7; color:#4b5563;'>
                                                Click the button below to create a new password. If you did not request this, you can safely ignore this email.
                                            </p>
                
                                            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'>
                                                {expirySection}
                                            </table>
                
                                            <table role='presentation' cellspacing='0' cellpadding='0' border='0' style='margin-bottom:24px;'>
                                                <tr>
                                                    <td align='center' style='border-radius:10px; background-color:#16a34a;'>
                                                        <a href='{resetPasswordLink}'
                                                           style='display:inline-block; padding:14px 28px; font-size:15px; font-weight:700; color:#ffffff; text-decoration:none; border-radius:10px;'>
                                                            Reset Password
                                                        </a>
                                                    </td>
                                                </tr>
                                            </table>
                
                                            <p style='margin:0 0 8px 0; font-size:13px; color:#6b7280;'>
                                                If the button does not work, copy and paste this link into your browser:
                                            </p>
                
                                            <p style='margin:0 0 24px 0; font-size:13px; line-height:1.7; word-break:break-all;'>
                                                <a href='{resetPasswordLink}' style='color:#2563eb; text-decoration:none;'>{resetPasswordLink}</a>
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
                                                This password reset email was sent by Spendwise. If you were not expecting this email, you can safely ignore it.
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
    }
}
