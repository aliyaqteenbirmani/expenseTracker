using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;

namespace ExpenseTrackingSystem.Application.Services.CashBookService
{
    public interface ICashBookService
    {
        Task<ApiResponse<object>> AddCashBook(CreateCashBookDto dto, string userId, string createdBy);
        Task<ApiResponse<List<CashBook>>> GetAllCashBooks(string userId);
        Task<ApiResponse<CashBook>> GetCashBookById(Guid id, string userId);
        Task<ApiResponse<object>> UpdateCashBook(Guid id, UpdateCashBookDto dto, string userId, string modifiedBy);
    }
}
