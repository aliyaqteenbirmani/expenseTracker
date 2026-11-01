using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Infrastructure.Data.Migrations;

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
    }
}
