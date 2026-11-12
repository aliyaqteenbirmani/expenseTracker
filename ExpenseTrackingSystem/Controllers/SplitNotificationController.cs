using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SpendwiseSystem.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SplitNotificationController : ControllerBase
    {
        private readonly ISplitNotificationService _notificationService;

        public SplitNotificationController(ISplitNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private Guid CurrentUserId
        {
            get
            {
                var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("id")?.Value
                    ?? User.FindFirst("sub")?.Value;

                return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedSplitNotificationResultDto>>> GetMyNotifications(
            [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _notificationService.GetMyNotificationsAsync(CurrentUserId, pageNumber, pageSize);
            return Ok(response);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<SplitNotificationUnreadCountDto>>> GetUnreadCount()
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _notificationService.GetUnreadCountAsync(CurrentUserId);
            return Ok(response);
        }

        [HttpPut("{id:guid}/read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id)
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _notificationService.MarkAsReadAsync(id, CurrentUserId);
            return Ok(response);
        }

        [HttpPut("read-all")]
        public async Task<ActionResult<ApiResponse<int>>> MarkAllAsRead()
        {
            if (CurrentUserId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var response = await _notificationService.MarkAllAsReadAsync(CurrentUserId);
            return Ok(response);
        }
    }
}
