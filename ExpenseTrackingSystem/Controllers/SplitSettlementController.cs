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
    [Route("api/splitgroup/{groupId:guid}/settlements")]
    [ApiController]
    [Authorize]
    public class SplitSettlementController : ControllerBase
    {
        private readonly ISplitSettlementService _settlementService;
        private readonly ICurrentUserService _currentUserService;

        public SplitSettlementController(
            ISplitSettlementService settlementService,
            ICurrentUserService currentUserService)
        {
            _settlementService = settlementService;
            _currentUserService = currentUserService;
        }

        private Guid CurrentUserId => _currentUserService.UserId ?? Guid.Empty;
        private string CurrentUserName => string.IsNullOrWhiteSpace(_currentUserService.UserName) ? "User" : _currentUserService.UserName;

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SplitSettlementDto>>> RecordSettlement(
            Guid groupId, [FromBody] RecordSettlementDto dto)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.RecordSettlementAsync(groupId, dto, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Created(string.Empty, response);
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<SplitSettlementDto>>>> GetSettlementHistory(Guid groupId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.GetGroupSettlementsAsync(groupId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("pending-actions")]
        public async Task<ActionResult<ApiResponse<List<SplitSettlementDto>>>> GetPendingActions(Guid groupId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.GetPendingActionsAsync(groupId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("suggestions")]
        public async Task<ActionResult<ApiResponse<SettleUpOverviewDto>>> GetSettleUpSuggestions(Guid groupId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.GetSettleUpOverviewAsync(groupId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{settlementId:guid}")]
        public async Task<ActionResult<ApiResponse<SplitSettlementDto>>> GetSettlementDetails(
            Guid groupId, Guid settlementId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.GetSettlementDetailsAsync(groupId, settlementId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("{settlementId:guid}/confirm")]
        public async Task<ActionResult<ApiResponse<SplitSettlementDto>>> ConfirmSettlement(
            Guid groupId, Guid settlementId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.ConfirmSettlementAsync(groupId, settlementId, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{settlementId:guid}/decline")]
        public async Task<ActionResult<ApiResponse<SplitSettlementDto>>> DeclineSettlement(
            Guid groupId, Guid settlementId, [FromBody] DeclineSettlementDto? dto)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.DeclineSettlementByReceiverAsync(
                groupId, settlementId, dto?.Reason, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{settlementId:guid}/approve")]
        public async Task<ActionResult<ApiResponse<SplitSettlementDto>>> ApproveSettlementByOwner(
            Guid groupId, Guid settlementId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.ApproveSettlementByOwnerAsync(
                groupId, settlementId, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{settlementId:guid}/owner-decline")]
        public async Task<ActionResult<ApiResponse<SplitSettlementDto>>> DeclineSettlementByOwner(
            Guid groupId, Guid settlementId, [FromBody] DeclineSettlementDto? dto)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.DeclineSettlementByOwnerAsync(
                groupId, settlementId, dto?.Reason, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{settlementId:guid}/cancel")]
        public async Task<ActionResult<ApiResponse<SplitSettlementDto>>> CancelSettlement(
            Guid groupId, Guid settlementId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.CancelSettlementAsync(
                groupId, settlementId, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{settlementId:guid}/reverse")]
        public async Task<ActionResult<ApiResponse<bool>>> ReverseSettlement(
            Guid groupId, Guid settlementId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.ReverseSettlementAsync(
                groupId, settlementId, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("{settlementId:guid}")]
        public async Task<ActionResult<ApiResponse<SplitSettlementDto>>> UpdateSettlement(
            Guid groupId, Guid settlementId, [FromBody] UpdateSettlementDto dto)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.UpdateSettlementAsync(groupId, settlementId, dto, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{settlementId:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteSettlement(
            Guid groupId, Guid settlementId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _settlementService.DeleteSettlementAsync(groupId, settlementId, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
