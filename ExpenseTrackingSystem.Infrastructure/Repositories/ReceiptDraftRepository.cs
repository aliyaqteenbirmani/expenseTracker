using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Infrastructure.Data.Migrations;
using Microsoft.EntityFrameworkCore;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class ReceiptDraftRepository : IReceiptDraftRepository
    {
        private readonly AppDbContext _dbContext;

        public ReceiptDraftRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(ReceiptDraft draft)
        {
            await _dbContext.ReceiptDrafts.AddAsync(draft);
            await _dbContext.SaveChangesAsync();
        }

        public Task<ReceiptDraft> GetOwnedAsync(Guid draftId, Guid userId, DateTime utcNow)
        {
            return _dbContext.ReceiptDrafts.AsNoTracking()
                .FirstOrDefaultAsync(draft => draft.Id == draftId
                    && draft.UserId == userId
                    && draft.ExpiresOn > utcNow);
        }

        public async Task UpdateOcrResultAsync(Guid draftId, string text, double confidence)
        {
            var draft = await _dbContext.ReceiptDrafts.FirstOrDefaultAsync(item => item.Id == draftId);
            if (draft == null)
                throw new InvalidOperationException("Receipt draft was not found.");

            draft.OcrText = text;
            draft.OcrConfidence = confidence;
            draft.Status = "OcrComplete";
            await _dbContext.SaveChangesAsync();
        }
    }
}
