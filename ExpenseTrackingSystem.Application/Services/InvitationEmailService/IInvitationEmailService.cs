using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.InvitationEmailService
{
    public interface IInvitationEmailService
    {
        Task<ApiResponse<bool>> SendBusinessInvitationEmailAsync(
            string toEmail,
            string invitedByName,
            string businessName,
            List<string> permissions,
            DateTime? expiresOn, 
            string remarks
            );

        Task<ApiResponse<bool>> SendCashbookInvitationEmailAsync(
            string toEmail,
            string invitedByName,
            string businessName,
            string cashbookName,
            List<string> permissions,
            DateTime? expiresOn,
            string remarks
            );
    }
}
