using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.Entities;
using SpendwiseEntity = SpendwiseSystem.Domain.Entities.Spendwise;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface ISpendwiseRepository
    {
        Task<SpendwiseCommandResultDbo> AddSpendwise(string spendwiseName, string userId, string createdBy);
        Task<List<SpendwiseEntity>> GetAllSpendwises(string userId);
        Task<SpendwiseEntity> GetSpendwiseById(Guid id, string userId);
        Task<SpendwiseCommandResultDbo> UpdateSpendwise(Guid id, string spendwiseName, string userId, string modifiedBy);
    }
}




