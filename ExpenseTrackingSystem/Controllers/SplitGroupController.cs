using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.CurrentUserService;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SplitGroupController : ControllerBase
    {
        private readonly ISplitGroupService _splitGroupService;
        private readonly ICurrentUserService _currentUserService;

        public SplitGroupController(
            ISplitGroupService splitGroupService,
            ICurrentUserService currentUserService)
        {
            _splitGroupService = splitGroupService;
            _currentUserService = currentUserService;
        }

        private Guid CurrentUserId => _currentUserService.UserId ?? Guid.Empty;
        private string CurrentUserName => string.IsNullOrWhiteSpace(_currentUserService.UserName) ? "User" : _currentUserService.UserName;
        private string CurrentUserEmail => _currentUserService.Email ?? string.Empty;

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SplitGroupSummaryDto>>> CreateGroup([FromBody] CreateSplitGroupDto dto)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.CreateGroupAsync(dto, CurrentUserId, CurrentUserName);
            if (!response.Success)
                return BadRequest(response);

            return Created(string.Empty, response);
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<SplitGroupSummaryDto>>>> GetMyGroups()
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.GetMyGroupsAsync(CurrentUserId);
            return Ok(response);
        }

        [HttpGet("{groupId:guid}")]
        public async Task<ActionResult<ApiResponse<SplitGroupDetailDto>>> GetGroupDetails(Guid groupId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.GetGroupDetailsAsync(groupId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{groupId:guid}/members")]
        public async Task<ActionResult<ApiResponse<List<SplitGroupMemberDto>>>> GetGroupMembers(Guid groupId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.GetGroupMembersAsync(groupId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{groupId:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateGroup(Guid groupId, [FromBody] UpdateSplitGroupDto dto)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.UpdateGroupAsync(groupId, dto, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{groupId:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteGroup(Guid groupId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.DeleteGroupAsync(groupId, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{groupId:guid}/invite")]
        public async Task<ActionResult<ApiResponse<SplitGroupInvitationDto>>> InviteMember(
            Guid groupId, [FromBody] InviteSplitGroupMemberDto dto)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.InviteMemberAsync(groupId, dto, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{groupId:guid}/invitations")]
        public async Task<ActionResult<ApiResponse<List<SplitGroupInvitationDto>>>> GetGroupPendingInvitations(Guid groupId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.GetPendingInvitationsAsync(groupId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("invitations/me")]
        public async Task<ActionResult<ApiResponse<List<SplitGroupInvitationDto>>>> GetMyInvitations()
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.GetMyInvitationsAsync(CurrentUserId, CurrentUserEmail);
            return Ok(response);
        }

        [HttpPost("{groupId:guid}/invitations/{invitationId:guid}/revoke")]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeInvitation(Guid groupId, Guid invitationId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.RevokeInvitationAsync(groupId, invitationId, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("invitations/{invitationId:guid}/accept")]
        public async Task<ActionResult<ApiResponse<bool>>> AcceptInvitation(Guid invitationId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.AcceptInvitationAsync(invitationId, CurrentUserId, CurrentUserEmail, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("invitations/{invitationId:guid}/reject")]
        public async Task<ActionResult<ApiResponse<bool>>> RejectInvitation(Guid invitationId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.RejectInvitationAsync(invitationId, CurrentUserId, CurrentUserEmail, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{groupId:guid}/members/{userId:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveMember(Guid groupId, Guid userId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.RemoveMemberAsync(groupId, userId, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{groupId:guid}/activities")]
        public async Task<ActionResult<ApiResponse<PagedGroupActivityResultDto>>> GetGroupActivities(
            Guid groupId,
            [FromQuery] SplitGroupActivityQueryParameters parameters)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.GetPagedGroupActivitiesAsync(groupId, parameters, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{groupId:guid}/activities/{activityId:guid}")]
        public async Task<ActionResult<ApiResponse<SplitGroupActivityDto>>> GetActivityById(
            Guid groupId,
            Guid activityId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _splitGroupService.GetActivityByIdAsync(groupId, activityId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                if (response.StatusCode == 404 || response.ErrorCode == "NOT_FOUND")
                    return NotFound(response);
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
