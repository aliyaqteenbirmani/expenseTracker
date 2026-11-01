using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Application.Services.ReceiptParsingService;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface IReceiptDraftRepository
    {
        Task AddAsync(ReceiptDraft draft);
        Task<ReceiptDraft> GetOwnedAsync(Guid draftId, Guid userId, DateTime utcNow);
        Task UpdateOcrResultAsync(Guid draftId, string text, double confidence);
        Task UpdateParsedSuggestionAsync(Guid draftId, ReceiptDraftSuggestion suggestion);
    }
}
