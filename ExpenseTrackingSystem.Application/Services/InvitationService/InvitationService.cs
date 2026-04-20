using Microsoft.Extensions.Logging;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.InvitationEmailService;
using SpendwiseSystem.Application.Services.InvitationLinkBuilder;
using SpendwiseSystem.Domain.DTOs.InvitationRequestDto;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.InvitationService
{
    public class InvitationService : IInvitationService
    {
        private readonly IInvitationRepository _invitationRepository;
        private readonly IInvitationEmailService _invitationEmailService;
        private readonly IInvitationLinkBuilder _invitationLinkBuilder;
        private readonly ILogger<InvitationService> _logger;

        public InvitationService(IInvitationRepository invitationRepository, IInvitationEmailService invitationEmailService, ILogger<InvitationService> logger, IInvitationLinkBuilder invitationLinkBuilder)
        {
            _invitationRepository = invitationRepository;
            _invitationEmailService = invitationEmailService;
            _logger = logger;
            _invitationLinkBuilder = invitationLinkBuilder;
        }

        public async Task<ApiResponse<Guid>> CreateBusinessInvitationAsync(
                    Guid businessId,
                    CreateBusinessInvitationRequest request,
                    Guid currentUserId)
        {
            if (businessId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Business id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Current user id is required.");

            if (request == null)
                return ApiResponse<Guid>.FailureResponse("Request is required.");

            if (string.IsNullOrWhiteSpace(request.InvitedEmail))
                return ApiResponse<Guid>.FailureResponse("Invited email is required.");

            if (request.Permissions == null || !request.Permissions.Any())
                return ApiResponse<Guid>.FailureResponse("At least one permission is required.");

            request.InvitedEmail = request.InvitedEmail.Trim().ToLower();

            request.Permissions = request.Permissions
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!PermissionValidationHelper.AreValidBusinessPermissions(request.Permissions))
                return ApiResponse<Guid>.FailureResponse("One or more invalid business permissions provided.");

            var inviteToken = InviteTokenHelper.GenerateSecureToken();
            var tokenExpiresOn = request.ExpiresOn ?? DateTime.UtcNow.AddDays(7); 

            var result = await _invitationRepository.CreateBusinessInvitationAsync(
                businessId,
                request.InvitedEmail,
                currentUserId,
                request.ExpiresOn,
                tokenExpiresOn,
                inviteToken,
                request.Remarks?.Trim(),
                request.Permissions);

            if (result.Success)
            {
                await SendBusinessInvitationEmailIfPossibleAsync(
                    businessId,
                    currentUserId,
                    request.InvitedEmail,
                    request.Permissions,
                    request.ExpiresOn,
                    request.Remarks?.Trim(),
                    inviteToken);
            }
            result.Message = result.Message + "and Email is sent successfully.";
            return result;
        }

        public async Task<ApiResponse<Guid>> CreateCashbookInvitationAsync(
            Guid businessId,
            Guid cashbookId,
            CreateCashbookInvitationRequest request,
            Guid currentUserId)
        {
            if (businessId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Business id is required.");

            if (cashbookId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Cashbook id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Current user id is required.");

            if (request == null)
                return ApiResponse<Guid>.FailureResponse("Request is required.");

            if (string.IsNullOrWhiteSpace(request.InvitedEmail))
                return ApiResponse<Guid>.FailureResponse("Invited email is required.");

            if (request.Permissions == null || !request.Permissions.Any())
                return ApiResponse<Guid>.FailureResponse("At least one permission is required.");

            request.InvitedEmail = request.InvitedEmail.Trim().ToLower();

            request.Permissions = request.Permissions
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!PermissionValidationHelper.AreValidCashbookPermissions(request.Permissions))
                return ApiResponse<Guid>.FailureResponse("One or more invalid cashbook permissions provided.");

            var inviteToken = InviteTokenHelper.GenerateSecureToken();
            var tokenExpiresOn = request.ExpiresOn ?? DateTime.UtcNow.AddDays(7);

            var result = await _invitationRepository.CreateCashbookInvitationAsync(
                businessId,
                cashbookId,
                request.InvitedEmail,
                currentUserId,
                request.ExpiresOn,
                tokenExpiresOn,
                inviteToken,
                request.Remarks?.Trim(),
                request.Permissions);

            if (result.Success)
            {
                await SendCashbookInvitationEmailIfPossibleAsync(
                    businessId,
                    cashbookId,
                    currentUserId,
                    request.InvitedEmail,
                    request.Permissions,
                    request.ExpiresOn,
                    request.Remarks?.Trim(),
                    inviteToken);
            }
            result.Message = result.Message + "and email is sent successfully.";
            return result;
        }

        public async Task<ApiResponse<List<InvitationListItem>>> GetMyPendingInvitationsAsync(
            Guid currentUserId,
            string currentUserEmail)
        {
            if (currentUserId == Guid.Empty)
                return ApiResponse<List<InvitationListItem>>.FailureResponse("Current user id is required.");

            if (string.IsNullOrWhiteSpace(currentUserEmail))
                return ApiResponse<List<InvitationListItem>>.FailureResponse("Current user email is required.");

            currentUserEmail = currentUserEmail.Trim().ToLower();

            var result = await _invitationRepository.GetMyPendingInvitationsAsync(
                currentUserId,
                currentUserEmail);

            return ApiResponse<List<InvitationListItem>>.SuccessResponse(
                "Pending invitations fetched successfully.",
                result);
        }

        public async Task<ApiResponse<Guid>> AcceptInvitationAsync(
            Guid invitationId,
            Guid currentUserId,
            string currentUserEmail)
        {
            if (invitationId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Invitation id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Current user id is required.");

            if (string.IsNullOrWhiteSpace(currentUserEmail))
                return ApiResponse<Guid>.FailureResponse("Current user email is required.");

            currentUserEmail = currentUserEmail.Trim().ToLower();

            var result = await _invitationRepository.AcceptInvitationAsync(
                invitationId,
                currentUserId,
                currentUserEmail);

            return result;
        }

        public async Task<ApiResponse<Guid>> RejectInvitationAsync(
            Guid invitationId,
            RejectInvitationRequest request,
            Guid currentUserId,
            string currentUserEmail)
        {
            if (invitationId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Invitation id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Current user id is required.");

            if (string.IsNullOrWhiteSpace(currentUserEmail))
                return ApiResponse<Guid>.FailureResponse("Current user email is required.");

            currentUserEmail = currentUserEmail.Trim().ToLower();

            var result = await _invitationRepository.RejectInvitationAsync(
                invitationId,
                currentUserId,
                currentUserEmail,
                request?.Remarks?.Trim());

            return result;
        }

        public async Task<ApiResponse<Guid>> RevokeInvitationAsync(
            Guid invitationId,
            Guid currentUserId)
        {
            if (invitationId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Invitation id is required.");

            if (currentUserId == Guid.Empty)
                return ApiResponse<Guid>.FailureResponse("Current user id is required.");

            var result = await _invitationRepository.RevokeInvitationAsync(
                invitationId,
                currentUserId);

            return result;
        }

        private async Task<object> SendBusinessInvitationEmailIfPossibleAsync(
            Guid businessId,
            Guid currentUserId,
            string invitedEmail,
            List<string> permissions,
            DateTime? expiresOn,
            string? remarks,
            string inviteToken)
        {
            try
            {
                var businessInfo = await _invitationRepository.GetBusinessEmailInfoAsync(businessId);
                var inviterInfo = await _invitationRepository.GetUserEmailInfoAsync(currentUserId);

                if (businessInfo != null && inviterInfo != null)
                {
                    var invitationLink = _invitationLinkBuilder.BuildInvitationLink(inviteToken);
                    var emailInfo = await _invitationEmailService.SendBusinessInvitationEmailAsync(
                        invitedEmail,
                        inviterInfo.FullName,
                        businessInfo.BusinessName,
                        permissions,
                        expiresOn,
                        remarks,
                        invitationLink);
                    return emailInfo;
                }
                return null;
            }
            catch(Exception ex) 
            {
                _logger.LogError(ex, "Failed to send business invitation email to {InvitedEmail} for business {BusinessId}", invitedEmail, businessId);
                return null;
            }
        }

        private async Task SendCashbookInvitationEmailIfPossibleAsync(
            Guid businessId,
            Guid cashbookId,
            Guid currentUserId,
            string invitedEmail,
            List<string> permissions,
            DateTime? expiresOn,
            string? remarks,
            string inviteToken)
        {
            try
            {
                var cashbookInfo = await _invitationRepository.GetCashbookEmailInfoAsync(businessId, cashbookId);
                var inviterInfo = await _invitationRepository.GetUserEmailInfoAsync(currentUserId);

                if (cashbookInfo == null || inviterInfo == null)
                    return;

                var invitationLink = _invitationLinkBuilder.BuildInvitationLink(inviteToken);

                await _invitationEmailService.SendCashbookInvitationEmailAsync(
                    invitedEmail,
                    inviterInfo.FullName,
                    cashbookInfo.BusinessName,
                    cashbookInfo.CashbookName,
                    permissions,
                    expiresOn,
                    remarks,
                    invitationLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send business invitation email to {InvitedEmail} for business {BusinessId}", invitedEmail, businessId);

            }
        }
    }
}
