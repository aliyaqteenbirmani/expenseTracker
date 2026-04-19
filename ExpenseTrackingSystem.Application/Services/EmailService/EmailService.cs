using Microsoft.Extensions.Options;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task<ApiResponse<bool>> SendEmailAsync(EmailMessage message)
        {
            try
            {
                var error = ValidateEmailMessage(message);
                if (error != null)
                    return ApiResponse<bool>.FailureResponse(error);

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
                    Subject = message.Subject,
                    Body = message.Body,
                    IsBodyHtml = message.IsBodyHtml
                };

                mailMessage.To.Add(message.ToEmail);

                using var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
                {
                    Credentials = new System.Net.NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password),
                    EnableSsl = _smtpSettings.EnableSsl
                };

                await smtpClient.SendMailAsync(mailMessage);
                return ApiResponse<bool>.SuccessResponse("Email sent successfully.", true);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.FailureResponse($"An error occurred while sending the email: {ex.Message}");
            }
        }


        private string ValidateEmailMessage(EmailMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.ToEmail))
                return "Recipient email is required.";

            if (string.IsNullOrWhiteSpace(message.Subject))
                return "Email subject is required.";

            if (string.IsNullOrWhiteSpace(message.Body))
                return "Email body is required.";

            return null;
        }
    }
}
