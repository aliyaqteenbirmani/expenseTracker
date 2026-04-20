using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Services.SpendwiseService
{
    public interface ICashBookService
    {
        Task<ApiResponse<object>> AddCashBook(CreateCashBookDto dto, string userId, string createdBy);
        Task<ApiResponse<List<CashBook>>> GetAllCashBook(string businessId);
        Task<ApiResponse<CashBook>> GetCashBookById(Guid id, string userId);
        Task<ApiResponse<CashBook>> UpdateCashBook(Guid id, UpdateCashBookDto dto, string userId, string modifiedBy);
        Task<ApiResponse<object>> DeleteCashBook(Guid id);
    }
}




