using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.PermissionAccessService;
using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.DTOs.CashBookDtos;
using SpendwiseSystem.Domain.DTOs.InvitationRequestDto;
using SpendwiseSystem.Domain.DTOs.MemberDto;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Domain.Enums;

namespace SpendwiseSystem.Application.Services.MemberManagementService
{
    public class MemberManagementService : IMemberManagementService
    {
        private readonly IMemberManagementRepository _memberManagementRepository;
        private readonly IPermissionAccessService _permissionAccessService;

        public MemberManagementService(
            IMemberManagementRepository memberManagementRepository,
            IPermissionAccessService permissionAccessService)
        {
            _memberManagementRepository = memberManagementRepository;
            _permissionAccessService = permissionAccessService;
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
            SpendwiseSystem.Domain.DTOs.InvitationRequestDto.UpdateBusinessMemberPermissionsRequest request,
            Guid currentUserId)
        {
            if (businessId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Business id is required.");

            if (userId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("User id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Current user id is required.");

            if (currentUserId == userId)
                return ApiResponse<Guid>.ForbiddenResponse("Members cannot modify their own permissions.");

            var isOwner = await _permissionAccessService.IsBusinessOwnerAsync(businessId, currentUserId);
            if (!isOwner)
                return ApiResponse<Guid>.ForbiddenResponse("Only the business owner can manage member permissions.");

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

            var permissionCodes = businessPermissions.Select(x => x.ToString()).ToList();

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
            SpendwiseSystem.Domain.DTOs.InvitationRequestDto.UpdateCashbookMemberPermissionsRequest request,
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

            if (currentUserId == userId)
                return ApiResponse<Guid>.ForbiddenResponse("Members cannot modify their own permissions.");

            var isOwner = await _permissionAccessService.IsCashbookOwnerAsync(cashbookId, currentUserId);
            if (!isOwner)
                return ApiResponse<Guid>.ForbiddenResponse("Only the cashbook owner can manage member permissions.");

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

            var permissionCodes = cashbookPermissions.Select(x => x.ToString()).ToList();

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

            if (currentUserId != userId)
            {
                var isOwner = await _permissionAccessService.IsBusinessOwnerAsync(businessId, currentUserId);
                if (!isOwner)
                    return ApiResponse<Guid>.ForbiddenResponse("Only the business owner can remove other members.");
            }

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

            if (currentUserId != userId)
            {
                var isOwner = await _permissionAccessService.IsCashbookOwnerAsync(cashbookId, currentUserId);
                if (!isOwner)
                    return ApiResponse<Guid>.ForbiddenResponse("Only the cashbook owner can remove members from this cashbook.");
            }

            var result = await _memberManagementRepository.RemoveCashbookMemberAsync(
                businessId,
                cashbookId,
                userId,
                currentUserId);

            return result;
        }

        public async Task<ApiResponse<UserWorkspaceAccessDto>> GetUserWorkspaceAccessAsync(
            Guid businessId,
            Guid? cashbookId,
            Guid currentUserId)
        {
            if (businessId == Guid.Empty)
                return ApiResponse<UserWorkspaceAccessDto>.FailureResponse("Business id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<UserWorkspaceAccessDto>.FailureResponse("Current user id is required.");

            var isOwner = await _permissionAccessService.IsBusinessOwnerAsync(businessId, currentUserId);
            if (isOwner)
            {
                return ApiResponse<UserWorkspaceAccessDto>.SuccessResponse(
                    "User access retrieved successfully.",
                    new UserWorkspaceAccessDto
                    {
                        IsOwner = true,
                        BusinessPermissions = Enum.GetNames<BusinessPermission>().ToList(),
                        CashbookPermissions = Enum.GetNames<CashbookPermission>().ToList()
                    });
            }

            var access = await _memberManagementRepository.GetUserWorkspaceAccessAsync(
                businessId,
                cashbookId,
                currentUserId);

            if (cashbookId.HasValue && await _permissionAccessService.IsCashbookOwnerAsync(cashbookId.Value, currentUserId))
            {
                access.IsOwner = true;
                access.CashbookPermissions = Enum.GetNames<SpendwiseSystem.Domain.Enums.CashbookPermission>().ToList();
            }

            return ApiResponse<UserWorkspaceAccessDto>.SuccessResponse(
                "User access retrieved successfully.",
                access);
        }
    }
}
