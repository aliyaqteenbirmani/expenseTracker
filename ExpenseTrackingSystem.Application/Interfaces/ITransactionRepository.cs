using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.CashTransactionDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ITransactionRepository
    {
        Task<ApiResponseRaw> AddCashTransaction(CashTransactionDto transactionDto, string createdBy);
        Task<SPResponseFromDb> UpdateCashTransaction(CashTransactionUpdateDto dto, string modifiedBy);
        Task<ApiResponseRaw> DeleteTransactionAsync(Guid id, string modifiedBy);
        Task<ApiResponse<CashTransaction>> GetTransactionAsync(Guid id);
        Task<ApiResponseRaw> GetAllTransactionsOfCashBook(string CashBookId);
        Task<SPResponseFromDb> GetTransactionFileName(string id);

    }
}
