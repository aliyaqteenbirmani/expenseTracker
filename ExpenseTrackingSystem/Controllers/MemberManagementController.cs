using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Services.CurrentUserService;
using SpendwiseSystem.Application.Services.MemberManagementService;
using SpendwiseSystem.Domain.DTOs.InvitationRequestDto;
using System.Security.Claims;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MemberManagementController : ControllerBase
    {
        private readonly IMemberManagementService _memberManagementService;
        private readonly ICurrentUserService _currentService;

        public MemberManagementController(IMemberManagementService memberManagementService, ICurrentUserService currentService)
        {
            _memberManagementService = memberManagementService;
            _currentService = currentService;
        }

        [HttpGet("businesses/{businessId:guid}/members")]
        public async Task<IActionResult> GetBusinessMembers(Guid businessId)
        {
            var currentUserId = _currentService.UserId ?? Guid.Empty;

            if (currentUserId == Guid.Empty)
                return Unauthorized("Invalid user.");

            var result = await _memberManagementService.GetBusinessMembersAsync(
                businessId,
                currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("businesses/{businessId:guid}/cashbooks/{cashbookId:guid}/members")]
        public async Task<IActionResult> GetCashbookMembers(Guid businessId, Guid cashbookId)
        {
            var currentUserId = _currentService.UserId ?? Guid.Empty;

            if (currentUserId == Guid.Empty)
                return Unauthorized("Invalid user.");

            var result = await _memberManagementService.GetCashbookMembersAsync(
                businessId,
                cashbookId,
                currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("businesses/{businessId:guid}/members/{userId:guid}/permissions")]
        public async Task<IActionResult> UpdateBusinessMemberPermissions(
            Guid businessId,
            Guid userId,
            [FromBody] UpdateBusinessMemberPermissionsRequest request)
        {
            var currentUserId = _currentService.UserId ?? Guid.Empty;

            if (currentUserId == Guid.Empty)
                return Unauthorized("Invalid user.");

            var result = await _memberManagementService.UpdateBusinessMemberPermissionsAsync(
                businessId,
                userId,
                request,
                currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("businesses/{businessId:guid}/cashbooks/{cashbookId:guid}/members/{userId:guid}/permissions")]
        public async Task<IActionResult> UpdateCashbookMemberPermissions(
            Guid businessId,
            Guid cashbookId,
            Guid userId,
            [FromBody] UpdateCashbookMemberPermissionsRequest request)
        {
            var currentUserId = _currentService.UserId ?? Guid.Empty;

            if (currentUserId == Guid.Empty)
                return Unauthorized("Invalid user.");

            var result = await _memberManagementService.UpdateCashbookMemberPermissionsAsync(
                businessId,
                cashbookId,
                userId,
                request,
                currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("businesses/{businessId:guid}/members/{userId:guid}")]
        public async Task<IActionResult> RemoveBusinessMember(Guid businessId, Guid userId)
        {
            var currentUserId = _currentService.UserId ?? Guid.Empty;

            if (currentUserId == Guid.Empty)
                return Unauthorized("Invalid user.");

            var result = await _memberManagementService.RemoveBusinessMemberAsync(
                businessId,
                userId,
                currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("businesses/{businessId:guid}/cashbooks/{cashbookId:guid}/members/{userId:guid}")]
        public async Task<IActionResult> RemoveCashbookMember(
            Guid businessId,
            Guid cashbookId,
            Guid userId)
        {
            var currentUserId = _currentService.UserId ?? Guid.Empty;

            if (currentUserId == Guid.Empty)
                return Unauthorized("Invalid user.");

            var result = await _memberManagementService.RemoveCashbookMemberAsync(
                businessId,
                cashbookId,
                userId,
                currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

    }
}
