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
    [Route("api/splitgroup/{groupId:guid}")]
    [ApiController]
    [Authorize]
    public class SplitBalanceController : ControllerBase
    {
        private readonly ISplitBalanceService _balanceService;
        private readonly ICurrentUserService _currentUserService;

        public SplitBalanceController(
            ISplitBalanceService balanceService,
            ICurrentUserService currentUserService)
        {
            _balanceService = balanceService;
            _currentUserService = currentUserService;
        }

        private Guid CurrentUserId => _currentUserService.UserId ?? Guid.Empty;

        [HttpGet("balances")]
        public async Task<ActionResult<ApiResponse<List<MemberBalanceDto>>>> GetGroupBalances(Guid groupId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _balanceService.GetGroupBalancesAsync(groupId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("balances/me")]
        public async Task<ActionResult<ApiResponse<MemberBalanceDto>>> GetCurrentUserBalance(Guid groupId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _balanceService.GetMemberBalanceAsync(groupId, CurrentUserId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("balances/{userId:guid}")]
        public async Task<ActionResult<ApiResponse<MemberBalanceDto>>> GetMemberBalance(Guid groupId, Guid userId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _balanceService.GetMemberBalanceAsync(groupId, userId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("financial-summary")]
        public async Task<ActionResult<ApiResponse<GroupFinancialSummaryDto>>> GetGroupFinancialSummary(Guid groupId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _balanceService.GetGroupFinancialSummaryAsync(groupId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
