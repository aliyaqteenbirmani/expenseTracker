using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Infrastructure.Data.DbContext;
using SpendwiseEntity = SpendwiseSystem.Domain.Entities.Spendwise;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class SpendwiseRepository : ISpendwiseRepository
    {
        private readonly IDapperContext _dapperContext;

        public SpendwiseRepository(IDapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<SpendwiseCommandResultDbo> AddSpendwise(string spendwiseName, string userId, string createdBy)
        {
            return await _dapperContext.GetSingleAsync<SpendwiseCommandResultDbo>(
                "sp_AddNewSpendwise",
                new
                {
                    SpendwiseName = spendwiseName,
                    UserId = userId,
                    CreatedBy = createdBy
                });
        }

        public async Task<List<SpendwiseEntity>> GetAllSpendwises(string userId)
        {
            return await _dapperContext.GetListAsync<SpendwiseEntity>(
                "sp_GetAllSpendwises",
                new { UserId = userId });
        }

        public async Task<SpendwiseEntity> GetSpendwiseById(Guid id, string userId)
        {
            return await _dapperContext.GetSingleAsync<SpendwiseEntity>(
                "sp_GetSpendwiseById",
                new
                {
                    Id = id,
                    UserId = userId
                });
        }

        public async Task<SpendwiseCommandResultDbo> UpdateSpendwise(Guid id, string spendwiseName, string userId, string modifiedBy)
        {
            return await _dapperContext.GetSingleAsync<SpendwiseCommandResultDbo>(
                "sp_UpdateSpendwise",
                new
                {
                    Id = id,
                    SpendwiseName = spendwiseName,
                    UserId = userId,
                    ModifiedBy = modifiedBy
                });
        }
    }
}




