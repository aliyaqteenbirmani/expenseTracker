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
        Task<ApiResponse<CashTransaction>> AddCashTransaction(CashTransactionDto transactionDto, string createdBy, Guid userId);

        Task<ApiResponse<List<AllCashTransactionDto>>> GetAllTransactionsOfCashBook(string CashBookId, Guid userId);
        Task<ApiResponse<Guid>> DeleteTransaction(Guid id, string modifiedBy, Guid userId);
        Task<ApiResponse<CashTransactionDto>> GetTransactionById(Guid Id, Guid userId);
        Task<ApiResponse<CashTransactionFileDto>> GetCashTransactionFile(string id, Guid userId);
        Task<ApiResponse<CTUpdateResponseDto>> UpdateCashTransaction(CashTransactionUpdateDto transactionUpdateDto, string modifiedBy, Guid userId);
    }
}
