using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface IReceiptDraftRepository
    {
        Task AddAsync(ReceiptDraft draft);
    }
}
