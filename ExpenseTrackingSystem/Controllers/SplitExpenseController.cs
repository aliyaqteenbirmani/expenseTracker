using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.CurrentUserService;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/splitgroup/{groupId:guid}/expenses")]
    [ApiController]
    [Authorize]
    public class SplitExpenseController : ControllerBase
    {
        private readonly ISplitExpenseService _expenseService;
        private readonly ICurrentUserService _currentUserService;

        public SplitExpenseController(
            ISplitExpenseService expenseService,
            ICurrentUserService currentUserService)
        {
            _expenseService = expenseService;
            _currentUserService = currentUserService;
        }

        private Guid CurrentUserId => _currentUserService.UserId ?? Guid.Empty;
        private string CurrentUserName => string.IsNullOrWhiteSpace(_currentUserService.UserName) ? "User" : _currentUserService.UserName;

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SplitExpenseDetailDto>>> AddExpense(
            Guid groupId, [FromBody] CreateSplitExpenseDto dto)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _expenseService.AddExpenseAsync(groupId, dto, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Created(string.Empty, response);
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedExpenseResultDto>>> GetExpensesList(
            Guid groupId, [FromQuery] SplitExpenseQueryParameters parameters)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _expenseService.GetExpensesListAsync(groupId, CurrentUserId, parameters);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("{expenseId:guid}")]
        public async Task<ActionResult<ApiResponse<SplitExpenseDetailDto>>> GetExpenseDetails(
            Guid groupId, Guid expenseId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _expenseService.GetExpenseDetailsAsync(groupId, expenseId, CurrentUserId);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{expenseId:guid}")]
        public async Task<ActionResult<ApiResponse<SplitExpenseDetailDto>>> EditExpense(
            Guid groupId, Guid expenseId, [FromBody] UpdateSplitExpenseDto dto)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _expenseService.EditExpenseAsync(groupId, expenseId, dto, CurrentUserId, CurrentUserName);
            if (!response.Success)
            {
                if (response.StatusCode == 403 || response.ErrorCode == "FORBIDDEN")
                    return StatusCode(403, response);
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{expenseId:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteExpense(
            Guid groupId, Guid expenseId)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _expenseService.DeleteExpenseAsync(groupId, expenseId, CurrentUserId, CurrentUserName);
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
