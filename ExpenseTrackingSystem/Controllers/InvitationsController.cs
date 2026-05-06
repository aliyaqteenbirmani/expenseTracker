using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.CurrentUserService;
using SpendwiseSystem.Application.Services.InvitationService;
using SpendwiseSystem.Domain.DTOs.InvitationRequestDto;
using SpendwiseSystem.Domain.Entities;
using System.Security.Claims;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvitationsController : ControllerBase
    {
        private readonly IInvitationService _invitationService;
        private readonly ICurrentUserService _currentUserService;

        public InvitationsController(IInvitationService invitationService, ICurrentUserService currentUserService)
        {
            _invitationService = invitationService;
            _currentUserService = currentUserService;
        }

        [HttpPost("business/{businessId:guid}")]
        public async Task<IActionResult> CreateBusinessInvitation(Guid businessId,[FromBody] CreateBusinessInvitationRequest request)
        {
            var currentUserId = _currentUserService.UserId ?? Guid.Empty;

            if(currentUserId == Guid.Empty)
                return Unauthorized("Unauthorized or Invalid");

            var result = await _invitationService.CreateBusinessInvitationAsync(businessId, request, currentUserId);
            if (!result.Success)
                return BadRequest( new ApiResponse<object> 
                {
                  Success = false, 
                  Message = result.Message,
                  StatusCode = StatusCodes.Status400BadRequest ,
                  ErrorCode = result.ErrorCode
                });

            return Ok( new ApiResponse<object> 
            { 
              Success = true, 
              Message = result.Message,
              StatusCode = StatusCodes.Status200OK 
            });
        }

        [HttpPost("business/{businessId:guid}/cashbooks/{cashbookId:guid}")]
        public async Task<IActionResult> CreateCashbookInvitation(Guid businessId, Guid cashbookId, [FromBody] CreateCashbookInvitationRequest request)
        {
            var currentUserId = _currentUserService.UserId ?? Guid.Empty;

            if(currentUserId == Guid.Empty)
                return Unauthorized("Unauthorized or Invalid");

            var result = await _invitationService.CreateCashbookInvitationAsync(businessId, cashbookId, request, currentUserId);
            if (!result.Success)
                return BadRequest( new ApiResponse<object> 
                { 
                  Success = false, 
                  Message = result.Message,
                  StatusCode = StatusCodes.Status400BadRequest ,
                  ErrorCode = result.ErrorCode
                });

            return Ok( new ApiResponse<object> 
            { 
              Success = true, 
              Message = result.Message,
              StatusCode = StatusCodes.Status200OK 
            });
        }

        [HttpGet("mypendingInvitations")]
        public async Task<IActionResult> GetMyPendingInvitationsOfSender()
        {
            var currentUserId = _currentUserService.UserId ?? Guid.Empty;
            var currentUserEmail = _currentUserService.Email;

            if (currentUserId == Guid.Empty || string.IsNullOrEmpty(currentUserEmail))
                return Unauthorized("Unauthorized or Invalid");

            var result = await _invitationService.GetMyPendingInvitationsAsync(currentUserId, currentUserEmail);

            if (!result.Success)
                return BadRequest( new ApiResponse<object> 
                {
                  Success = false, 
                  Message = result.Message,
                  StatusCode = StatusCodes.Status400BadRequest ,
                  ErrorCode = result.ErrorCode
                });

            return Ok( new ApiResponse<object> 
            { 
              Success = true, 
              Message = result.Message,
              StatusCode = StatusCodes.Status200OK,
              Data = result.Data
            });
        }

        [HttpGet("mysentInvitations")]
        public async Task<IActionResult> GetSentInvitations(string status)
        {
            var currentUserId = _currentUserService.UserId ?? Guid.Empty;

            if (currentUserId == Guid.Empty)
                return Unauthorized("Unauthorized or Invalid");

            var result = await _invitationService.GetSentInvitationsAsync(currentUserId, status);

            if (!result.Success)
                return BadRequest( ApiResponses.BadRequest(result.Message, result.ErrorCode));

            return Ok(ApiResponses.SuccessWithData(result.Data));
        }

        [HttpGet("by-token")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInvitationByToken([FromQuery] string token)
        {
            var result = await _invitationService.GetInvitationByTokenAsync(token);
            if (!result.Success)
                return BadRequest( new ApiResponse<object> 
                { 
                  Success = false, 
                  Message = result.Message,
                  StatusCode = StatusCodes.Status400BadRequest ,
                  ErrorCode = result.ErrorCode
                });

            return Ok( new ApiResponse<object> 
            { 
              Success = true, 
              Message = result.Message,
              StatusCode = StatusCodes.Status200OK,
              Data = result.Data
            }); 
        }

        [HttpPost("{invitationId:guid}/accept")]
        public async Task<IActionResult> AcceptInvitation(Guid invitationId)
        {
            var currentUserId = _currentUserService.UserId ?? Guid.Empty;
            var currentUserEmail = _currentUserService.Email;

            if (currentUserId == Guid.Empty || string.IsNullOrWhiteSpace(currentUserEmail))
                return Unauthorized("Invalid user.");
        
            var result = await _invitationService.AcceptInvitationAsync(invitationId, currentUserId, currentUserEmail);

            if (!result.Success)
                return BadRequest( new ApiResponse<object> 
                { 
                  Success = false, 
                  Message = result.Message,
                  StatusCode = StatusCodes.Status400BadRequest ,
                  ErrorCode = result.ErrorCode
                });

            return Ok( new ApiResponse<object> 
            {
              Success = true, 
              Message = result.Message,
              StatusCode = StatusCodes.Status200OK 
            });
        }

        [HttpPost("{invitationId:guid}/reject")]
        public async Task<IActionResult> RejectInvitation(
            Guid invitationId,
            [FromBody] RejectInvitationRequest request)
        {
            var currentUserId = _currentUserService.UserId ?? Guid.Empty;
            var currentUserEmail = _currentUserService.Email;

            if (currentUserId == Guid.Empty || string.IsNullOrWhiteSpace(currentUserEmail))
                return Unauthorized("Invalid user.");

            var result = await _invitationService.RejectInvitationAsync(
                invitationId,
                request,
                currentUserId,
                currentUserEmail);

            if (!result.Success)
                return BadRequest( new ApiResponse<object> 
                { 
                  Success = false, 
                  Message = result.Message,
                  StatusCode = StatusCodes.Status400BadRequest ,
                  ErrorCode = result.ErrorCode
                });

            return Ok( new ApiResponse<object> 
            { 
              Success = true, 
              Message = result.Message,
              StatusCode = StatusCodes.Status200OK 
            });
        }


        [HttpPost("{invitationId:guid}/revoke")]
        public async Task<IActionResult> RevokeInvitation(Guid invitationId)
        {
            var currentUserId = _currentUserService.UserId ?? Guid.Empty;

            if (currentUserId == Guid.Empty)
                return Unauthorized("Invalid user.");

            var result = await _invitationService.RevokeInvitationAsync(
                invitationId,
                currentUserId);

            if (!result.Success)
                return BadRequest( new ApiResponse<object> 
                { 
                  Success = false, 
                  Message = result.Message,
                  StatusCode = StatusCodes.Status400BadRequest ,
                  ErrorCode = result.ErrorCode
                });

            return Ok( new ApiResponse<object> 
            { 
              Success = true, 
              Message = result.Message,
              StatusCode = StatusCodes.Status200OK 
            });
        }

    }
}
