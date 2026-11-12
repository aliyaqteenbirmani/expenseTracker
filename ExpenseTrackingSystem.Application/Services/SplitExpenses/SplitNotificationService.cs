using System;
using System.Threading.Tasks;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Services.SplitExpenses
{
    public class SplitNotificationService : ISplitNotificationService
    {
        private readonly ISplitNotificationRepository _notificationRepository;

        public SplitNotificationService(ISplitNotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse<PagedSplitNotificationResultDto>> GetMyNotificationsAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var result = await _notificationRepository.GetNotificationsByUserAsync(userId, pageNumber, pageSize);
                return ApiResponse<PagedSplitNotificationResultDto>.SuccessResponse("Notifications retrieved successfully.", result);
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedSplitNotificationResultDto>.FailureResponse($"Failed to retrieve notifications: {ex.Message}");
            }
        }

        public async Task<ApiResponse<SplitNotificationUnreadCountDto>> GetUnreadCountAsync(Guid userId)
        {
            try
            {
                var count = await _notificationRepository.GetUnreadCountAsync(userId);
                return ApiResponse<SplitNotificationUnreadCountDto>.SuccessResponse("Unread count retrieved.", new SplitNotificationUnreadCountDto { UnreadCount = count });
            }
            catch (Exception ex)
            {
                return ApiResponse<SplitNotificationUnreadCountDto>.FailureResponse($"Failed to get unread count: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            try
            {
                var updated = await _notificationRepository.MarkAsReadAsync(notificationId, userId);
                return updated
                    ? ApiResponse<bool>.SuccessResponse("Notification marked as read.", true)
                    : ApiResponse<bool>.SuccessResponse("Notification was already marked as read.", false);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.FailureResponse($"Failed to update notification: {ex.Message}");
            }
        }

        public async Task<ApiResponse<int>> MarkAllAsReadAsync(Guid userId)
        {
            try
            {
                var count = await _notificationRepository.MarkAllAsReadAsync(userId);
                return ApiResponse<int>.SuccessResponse($"Marked {count} notifications as read.", count);
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.FailureResponse($"Failed to mark notifications as read: {ex.Message}");
            }
        }
    }
}
