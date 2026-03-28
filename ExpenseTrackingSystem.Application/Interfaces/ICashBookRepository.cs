using ExpenseTrackingSystem.Domain.DBOs;
using ExpenseTrackingSystem.Domain.Entities;

namespace ExpenseTrackingSystem.Application.Interfaces
{
    public interface ICashBookRepository
    {
        Task<CashBookCommandResultDbo> AddCashBook(string bookName, string userId, string createdBy);
        Task<List<CashBook>> GetAllCashBooks(string userId);
        Task<CashBook> GetCashBookById(Guid id, string userId);
        Task<CashBookCommandResultDbo> UpdateCashBook(Guid id, string bookName, string userId, string modifiedBy);
    }
}
