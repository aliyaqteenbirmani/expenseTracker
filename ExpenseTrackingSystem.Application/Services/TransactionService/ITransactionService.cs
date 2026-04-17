using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.CashTransactionDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.TransactionService
{
    public interface ITransactionService
    {
        Task<ApiResponse<CashTransaction>> AddCashTransaction(CashTransactionDto transactionDto, string createdBy);

        Task<ApiResponse<List<AllCashTransactionDto>>> GetAllTransactionsOfCashBook(string CashBookId);
        Task<ApiResponseRaw> DeleteTransaction(Guid id, string modifiedBy);
        Task<ApiResponse<CashTransactionDto>> GetTransactionById(Guid Id);
        Task<ApiResponse<CashTransactionFileDto>> GetCashTransactionFile(string id);
        Task<ApiResponse<CTUpdateResponseDto>> UpdateCashTransaction(CashTransactionUpdateDto transactionUpdateDto, string modifiedBy);
    }
}
