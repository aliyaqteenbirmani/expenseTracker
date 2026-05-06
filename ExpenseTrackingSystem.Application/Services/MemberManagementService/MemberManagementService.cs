using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.DTOs.CashBookDtos;
using SpendwiseSystem.Domain.DTOs.InvitationRequestDto;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Services.MemberManagementService
{
    public class MemberManagementService : IMemberManagementService
    {
        private readonly IMemberManagementRepository _memberManagementRepository;

        public MemberManagementService(IMemberManagementRepository memberManagementRepository)
        {
            _memberManagementRepository = memberManagementRepository;
        }


        public async Task<ApiResponse<List<BusinessMemberDto>>> GetBusinessMembersAsync(
            Guid businessId,
            Guid currentUserId)
        {
            if (businessId == Guid.Empty)
                return ApiResponse<List<BusinessMemberDto>>.FailureResponse("Business id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<List<BusinessMemberDto>>.FailureResponse("Current user id is required.");

            var result = await _memberManagementRepository.GetBusinessMembersAsync(
                businessId,
                currentUserId);

            return ApiResponse<List<BusinessMemberDto>>.SuccessResponse(
                "Business members fetched successfully.",
                result);
        }

        public async Task<ApiResponse<List<CashBookMemberDto>>> GetCashbookMembersAsync(
            Guid businessId,
            Guid cashbookId,
            Guid currentUserId)
        {
            if (businessId == Guid.Empty)
                return ApiResponse<List<CashBookMemberDto>>.FailureResponse("Business id is required.");

            if (cashbookId == Guid.Empty)
                return ApiResponse<List<CashBookMemberDto>>.FailureResponse("Cashbook id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<List<CashBookMemberDto>>.FailureResponse("Current user id is required.");

            var result = await _memberManagementRepository.GetCashbookMembersAsync(
                businessId,
                cashbookId,
                currentUserId);

            return ApiResponse<List<CashBookMemberDto>>.SuccessResponse(
                "Cashbook members fetched successfully.",
                result);
        }

        public async Task<ApiResponse<Guid>> UpdateBusinessMemberPermissionsAsync(
            Guid businessId,
            Guid userId,
            UpdateBusinessMemberPermissionsRequest request,
            Guid currentUserId)
        {
            if (businessId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Business id is required.");

            if (userId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("User id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Current user id is required.");

            if (request == null)
                return ApiResponse<Guid>.FailureResponse("Request is required.");

            if (request.Permissions == null || !request.Permissions.Any())
                return ApiResponse<Guid>.FailureResponse("At least one permission is required.");

            request.Permissions = request.Permissions
                .Where(x => x >= 0)
                .Distinct()
                .ToList();

            var businessPermissions = request.Permissions
                .Select(x => (SpendwiseSystem.Domain.Enums.BusinessPermission)x);

            if (!PermissionValidationHelper.AreValidBusinessPermissions(businessPermissions))
                return ApiResponse<Guid>.FailureResponse("One or more invalid business permissions provided.");

            // Convert int permissions to string codes for repository
            var permissionCodes = request.Permissions.Select(x => x.ToString()).ToList();

            var result = await _memberManagementRepository.UpdateBusinessMemberPermissionsAsync(
                businessId,
                userId,
                currentUserId,
                permissionCodes);

            return result;
        }

        public async Task<ApiResponse<Guid>> UpdateCashbookMemberPermissionsAsync(
            Guid businessId,
            Guid cashbookId,
            Guid userId,
            UpdateCashbookMemberPermissionsRequest request,
            Guid currentUserId)
        {
            if (businessId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Business id is required.");

            if (cashbookId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Cashbook id is required.");

            if (userId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("User id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Current user id is required.");

            if (request == null)
                return ApiResponse<Guid>.FailureResponse("Request is required.");

            if (request.Permissions == null || !request.Permissions.Any())
                return ApiResponse<Guid>.FailureResponse("At least one permission is required.");

            request.Permissions = request.Permissions
                .Where(x => x >= 0)
                .Distinct()
                .ToList();

            var cashbookPermissions = request.Permissions
                .Select(x => (SpendwiseSystem.Domain.Enums.CashbookPermission)x);

            if (!PermissionValidationHelper.AreValidCashbookPermissions(cashbookPermissions))
                return ApiResponse<Guid>.FailureResponse("One or more invalid cashbook permissions provided.");

            var permissionCodes = request.Permissions.Select(x => x.ToString()).ToList();

            var result = await _memberManagementRepository.UpdateCashbookMemberPermissionsAsync(
                businessId,
                cashbookId,
                userId,
                currentUserId,
                permissionCodes);

            return result;
        }

        public async Task<ApiResponse<Guid>> RemoveBusinessMemberAsync(
            Guid businessId,
            Guid userId,
            Guid currentUserId)
        {
            if (businessId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Business id is required.");

            if (userId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("User id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Current user id is required.");

            var result = await _memberManagementRepository.RemoveBusinessMemberAsync(
                businessId,
                userId,
                currentUserId);

            return result;
        }

        public async Task<ApiResponse<Guid>> RemoveCashbookMemberAsync(
            Guid businessId,
            Guid cashbookId,
            Guid userId,
            Guid currentUserId)
        {
            if (businessId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Business id is required.");

            if (cashbookId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Cashbook id is required.");

            if (userId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("User id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Current user id is required.");

            var result = await _memberManagementRepository.RemoveCashbookMemberAsync(
                businessId,
                cashbookId,
                userId,
                currentUserId);

            return result;
        }
    }
}
