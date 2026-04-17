using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.CashTransactionDtos;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Infrastructure.Data.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IDapperContext _dapperContext;

        public TransactionRepository(IDapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<ApiResponseRaw> AddCashTransaction(CashTransactionDto transactionDto,string createdBy)
        {
            return await _dapperContext.GetSingleAsync<ApiResponseRaw>(
                        "SP_AddCashTransaction",
                        new
                        {
                            CashBookId = transactionDto.CashBookId,
                            Amount = transactionDto.Amount,
                            TransactionType = transactionDto.TransactionType,
                            Remarks = transactionDto.Remarks,
                            FileName = transactionDto.FileName,
                            CreatedBy = createdBy
                        });
        }
        public async Task<ApiResponseRaw> DeleteTransactionAsync(Guid id, string ModifiedBy)
        {
            return await _dapperContext.GetSingleAsync<ApiResponseRaw>("SP_DeleteCashTransaction",
                new { CashTransactionId = id,
                    ModifiedBy});
        }

        public async Task<ApiResponseRaw> GetAllTransactionsOfCashBook(string CashBookId)
        {
            return await _dapperContext.GetSingleAsync<ApiResponseRaw>(
                "SP_GetAllCashTransactionsForCashBook",
                new { CashBookId = Guid.Parse(CashBookId) },
                commandType: System.Data.CommandType.StoredProcedure
                );
        }

        public async Task<ApiResponse<CashTransaction>> GetTransactionAsync(Guid id)
        {
            var responseFromDb = await _dapperContext.GetSingleAsync<CashTransaction>(
                "SP_GetCashTransactionById",
                new { CashTransactionId = id }
                );

            if(responseFromDb is not null)
                return new ApiResponse<CashTransaction>
                {
                    Success = true,
                    StatusCode = ApiResponses.Success().StatusCode,
                    Data = responseFromDb
                };

            return new ApiResponse<CashTransaction>
            {
                Success = false,
                StatusCode = ApiResponses.BadRequest().StatusCode,
                Data = null
            };

        }

        public async Task<SPResponseFromDb> GetTransactionFileName(string id)
        {
            return await _dapperContext.GetSingleAsync<SPResponseFromDb>("SP_GetCashTransactionFileNameById",
                new { Id = Guid.Parse(id) },
                commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<SPResponseFromDb> UpdateCashTransaction(CashTransactionUpdateDto dto, string modifiedBy)
        {
            return await _dapperContext.GetSingleAsync<SPResponseFromDb>("SP_UpdateCashTransaction",
                new
                {
                    CashTransactionId = dto.Id,
                    Amount = dto.Amount,
                    TransactionType = dto.TransactionType,
                    Remarks = dto.Remarks,
                    FileName = dto.FileName,
                    ModifiedBy = modifiedBy
                },
                commandType: System.Data.CommandType.StoredProcedure);
        }
    }
}
