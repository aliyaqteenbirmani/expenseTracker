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
            var bId = Guid.TryParse(dto?.BusinessId, out var bid) ? bid : Guid.Empty;
            var uId = Guid.TryParse(userId, out var uid) ? uid : Guid.Empty;
            return await _dapperContext.GetSingleAsync<CashBookCommandResultDbo>(
                "SP_AddCashBook",
                new
                {
                    Name = dto?.CashBookName,
                    BusinessId = bId,
                    UserId = uId,
                    CreatedBy = createdBy
                });
        }

        public async Task<ApiResponseRaw> GetAllCashBooks(string businessId, string? userId = null)
        {
            var pUserId = string.IsNullOrWhiteSpace(userId) ? (Guid?)null : (Guid.TryParse(userId, out var uid) ? uid : (Guid?)null);
            var pBusinessId = Guid.TryParse(businessId, out var bid) ? bid : Guid.Empty;
            return await _dapperContext.GetSingleAsync<ApiResponseRaw>(
                "SP_GetCashBooksByBusinessId",
                new
                {
                    BusinessId = pBusinessId,
                    UserId = pUserId
                });
        }

        public async Task<ApiResponseRaw> GetCashBookById(Guid id, string userId)
        {
            var uId = Guid.TryParse(userId, out var uid) ? uid : Guid.Empty;
            return await _dapperContext.GetSingleAsync<ApiResponseRaw>(
                "sp_GetCashBookById",
                new
                {
                    Id = id,
                    UserId = uId
                });
        }

        public async Task<ApiResponseRaw> UpdateCashBook(Guid id, string CashBookName, string userId, string modifiedBy)
        {
            var uId = Guid.TryParse(userId, out var uid) ? uid : Guid.Empty;
            return await _dapperContext.GetSingleAsync<ApiResponseRaw>(
                "SP_UpdateCashBook",
                new
                {
                    Id = id,
                    CashBookName = CashBookName,
                    UserId = uId,
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
