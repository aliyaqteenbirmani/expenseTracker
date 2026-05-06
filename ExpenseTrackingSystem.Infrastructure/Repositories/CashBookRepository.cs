

using CashBookSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.CashBookDtos;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Infrastructure.Data.DbContext;

namespace CashBookSystem.Infrastructure.Repositories
{
    public class CashBookRepository : ICashBookRepository
    {
        private readonly IDapperContext _dapperContext;

        public CashBookRepository(IDapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<CashBookCommandResultDbo> AddCashBook(CreateCashBookDto dto, string userId, string createdBy)
        {
            return await _dapperContext.GetSingleAsync<CashBookCommandResultDbo>(
                "SP_AddCashBook",
                new
                {
                    Name = dto.CashBookName,
                    BusinessId = Guid.Parse(dto.BusinessId),
                    UserId = Guid.Parse(userId),
                    CreatedBy = createdBy
                });
        }

        public async Task<ApiResponseRaw> GetAllCashBooks(string businessId)
        {
            return await _dapperContext.GetSingleAsync<ApiResponseRaw>(
                "SP_GetCashBooksByBusinessId",
                new { BusinessId = Guid.Parse(businessId) });
        }

        public async Task<ApiResponseRaw> GetCashBookById(Guid id, string userId)
        {
            return await _dapperContext.GetSingleAsync<ApiResponseRaw>(
                "sp_GetCashBookById",
                new
                {
                    Id = id,
                    UserId = Guid.Parse(userId)
                });
        }

        public async Task<ApiResponseRaw> UpdateCashBook(Guid id, string CashBookName, string userId, string modifiedBy)
        {
            return await _dapperContext.GetSingleAsync<ApiResponseRaw>(
                "SP_UpdateCashBook",
                new
                {
                    Id = id,
                    CashBookName = CashBookName,
                    UserId = Guid.Parse(userId),
                    ModifiedBy = modifiedBy
                });
        }

        public async Task<ApiResponseRaw> DeleteCashBook(Guid id)
        {
            return await _dapperContext.GetSingleAsync<ApiResponseRaw>(
                "SP_DeleteCashBook",
                new { CashBookId = id });
        }
    }
}




