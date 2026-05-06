using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.DTOs.CashBookDtos;
using SpendwiseSystem.Domain.DTOs.InvitationRequestDto;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.MemberManagementService
{
    public interface IMemberManagementService
    {
        Task<ApiResponse<List<BusinessMemberDto>>> GetBusinessMembersAsync(
            Guid businessId,
            Guid currentUserId);

        Task<ApiResponse<List<CashBookMemberDto>>> GetCashbookMembersAsync(
            Guid businessId,
            Guid cashbookId,
            Guid currentUserId);

        Task<ApiResponse<Guid>> UpdateBusinessMemberPermissionsAsync(
            Guid businessId,
            Guid userId,
            UpdateBusinessMemberPermissionsRequest request,
            Guid currentUserId);

        Task<ApiResponse<Guid>> UpdateCashbookMemberPermissionsAsync(
            Guid businessId,
            Guid cashbookId,
            Guid userId,
            UpdateCashbookMemberPermissionsRequest request,
            Guid currentUserId);

        Task<ApiResponse<Guid>> RemoveBusinessMemberAsync(
            Guid businessId,
            Guid userId,
            Guid currentUserId);

        Task<ApiResponse<Guid>> RemoveCashbookMemberAsync(
            Guid businessId,
            Guid cashbookId,
            Guid userId,
            Guid currentUserId);
    }
}
