
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;

namespace CashBookSystem.Application.Interfaces
{
    public interface ICashBookRepository
    {
        Task<CashBookCommandResultDbo> AddCashBook(CreateCashBookDto dto, string userId, string createdBy);
        Task<ApiResponseRaw> GetAllCashBooks(string userId);
        Task<ApiResponseRaw> GetCashBookById(Guid id, string userId);
        Task<ApiResponseRaw> UpdateCashBook(Guid id, string CashBookName, string userId, string modifiedBy);
        Task<ApiResponseRaw> DeleteCashBook(Guid id);
    }
}




