using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpendwiseSystem.Domain.DTOs.SplitExpenseDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitNotificationRepository
    {
        Task<PagedSplitNotificationResultDto> GetNotificationsByUserAsync(Guid userId, int pageNumber = 1, int pageSize = 20);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
        Task<int> MarkAllAsReadAsync(Guid userId);
        Task<SplitNotification> CreateNotificationAsync(SplitNotification notification);
        Task<bool> MarkInvitationNotificationAsReadAsync(Guid invitationId, Guid userId);
    }
}
