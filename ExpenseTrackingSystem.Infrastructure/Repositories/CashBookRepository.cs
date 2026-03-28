using ExpenseTrackingSystem.Application.Interfaces;
using ExpenseTrackingSystem.Domain.DBOs;
using ExpenseTrackingSystem.Domain.Entities;
using ExpenseTrackingSystem.Infrastructure.Data.DbContext;

namespace ExpenseTrackingSystem.Infrastructure.Repositories
{
    public class CashBookRepository : ICashBookRepository
    {
        private readonly IDapperContext _dapperContext;

        public CashBookRepository(IDapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<CashBookCommandResultDbo> AddCashBook(string bookName, string userId, string createdBy)
        {
            return await _dapperContext.GetSingleAsync<CashBookCommandResultDbo>(
                "sp_AddNewCashBook",
                new
                {
                    BookName = bookName,
                    UserId = userId,
                    CreatedBy = createdBy
                });
        }

        public async Task<List<CashBook>> GetAllCashBooks(string userId)
        {
            return await _dapperContext.GetListAsync<CashBook>(
                "sp_GetAllCashBooks",
                new { UserId = userId });
        }

        public async Task<CashBook> GetCashBookById(Guid id, string userId)
        {
            return await _dapperContext.GetSingleAsync<CashBook>(
                "sp_GetCashBookById",
                new
                {
                    Id = id,
                    UserId = userId
                });
        }

        public async Task<CashBookCommandResultDbo> UpdateCashBook(Guid id, string bookName, string userId, string modifiedBy)
        {
            return await _dapperContext.GetSingleAsync<CashBookCommandResultDbo>(
                "sp_UpdateCashBook",
                new
                {
                    Id = id,
                    BookName = bookName,
                    UserId = userId,
                    ModifiedBy = modifiedBy
                });
        }
    }
}
