using System;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitNotificationService
    {
        Task<ApiResponse<PagedSplitNotificationResultDto>> GetMyNotificationsAsync(Guid userId, int pageNumber = 1, int pageSize = 20);
        Task<ApiResponse<SplitNotificationUnreadCountDto>> GetUnreadCountAsync(Guid userId);
        Task<ApiResponse<bool>> MarkAsReadAsync(Guid notificationId, Guid userId);
        Task<ApiResponse<int>> MarkAllAsReadAsync(Guid userId);
    }
}
