using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;
using SpendwiseEntity = SpendwiseSystem.Domain.Entities.Spendwise;

namespace SpendwiseSystem.Application.Services.SpendwiseService
{
    public interface ISpendwiseService
    {
        Task<ApiResponse<object>> AddSpendwise(CreateSpendwiseDto dto, string userId, string createdBy);
        Task<ApiResponse<List<SpendwiseEntity>>> GetAllSpendwises(string userId);
        Task<ApiResponse<SpendwiseEntity>> GetSpendwiseById(Guid id, string userId);
        Task<ApiResponse<object>> UpdateSpendwise(Guid id, UpdateSpendwiseDto dto, string userId, string modifiedBy);
    }
}




