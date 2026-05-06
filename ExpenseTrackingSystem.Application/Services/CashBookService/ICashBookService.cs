using SpendwiseSystem.Domain.DTOs.CashBookDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Services.SpendwiseService
{
    public interface ICashBookService
    {
        Task<ApiResponse<object>> AddCashBook(CreateCashBookDto dto, string userId, string createdBy);
        Task<ApiResponse<List<CreateCashBookDto>>> GetAllCashBook(string businessId,string userId);
        Task<ApiResponse<object>> GetCashBookById(Guid id, string userId);
        Task<ApiResponse<CashBook>> UpdateCashBook(Guid id, UpdateCashBookDto dto, string userId, string modifiedBy);
        Task<ApiResponse<object>> DeleteCashBook(string id, string userId);
    }
}




