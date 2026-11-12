using System;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISplitRealtimeService
    {
        Task NotifyUserAsync(Guid userId, string eventName, object payload);
        Task NotifyEmailAsync(string email, string eventName, object payload);
    }
}
