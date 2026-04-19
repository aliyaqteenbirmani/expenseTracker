using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.EmailService
{
    public interface IEmailService
    {
        Task<ApiResponse<bool>> SendEmailAsync(EmailMessage message);
    }
}
