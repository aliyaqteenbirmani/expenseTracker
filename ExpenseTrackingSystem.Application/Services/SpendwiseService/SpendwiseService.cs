using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;
using SpendwiseEntity = SpendwiseSystem.Domain.Entities.Spendwise;

namespace SpendwiseSystem.Application.Services.SpendwiseService
{
    public class SpendwiseService : ISpendwiseService
    {
        private readonly ISpendwiseRepository _spendwiseRepository;

        public SpendwiseService(ISpendwiseRepository spendwiseRepository)
        {
            _spendwiseRepository = spendwiseRepository;
        }

        public async Task<ApiResponse<object>> AddSpendwise(CreateSpendwiseDto dto, string userId, string createdBy)
        {
            var result = await _spendwiseRepository.AddSpendwise(dto.SpendwiseName, userId, createdBy);

            return result?.ResponseCode switch
            {
                "200" => ApiResponses.Success(result.ResponseMessage),
                "409" => ApiResponses.Conflict(result.ResponseMessage),
                _ => ApiResponses.InternalServerError(result?.ResponseMessage ?? "Unable to create spendwise")
            };
        }

        public async Task<ApiResponse<List<SpendwiseEntity>>> GetAllSpendwises(string userId)
        {
            var spendwiseItems = await _spendwiseRepository.GetAllSpendwises(userId);
            return ApiResponses.SuccessWithData(spendwiseItems ?? new List<SpendwiseEntity>(), "Spendwise fetched successfully");
        }

        public async Task<ApiResponse<SpendwiseEntity>> GetSpendwiseById(Guid id, string userId)
        {
            var spendwise = await _spendwiseRepository.GetSpendwiseById(id, userId);

            if (spendwise is null)
            {
                return new ApiResponse<SpendwiseEntity>
                {
                    StatusCode = 404,
                    Success = false,
                    Message = "Spendwise not found",
                    ErrorCode = "NOT_FOUND",
                    Data = null
                };
            }

            return ApiResponses.SuccessWithData(spendwise, "Spendwise fetched successfully");
        }

        public async Task<ApiResponse<object>> UpdateSpendwise(Guid id, UpdateSpendwiseDto dto, string userId, string modifiedBy)
        {
            var result = await _spendwiseRepository.UpdateSpendwise(id, dto.SpendwiseName, userId, modifiedBy);

            return result?.ResponseCode switch
            {
                "200" => ApiResponses.Success(result.ResponseMessage),
                "404" => ApiResponses.NotFound(result.ResponseMessage),
                "409" => ApiResponses.Conflict(result.ResponseMessage),
                _ => ApiResponses.InternalServerError(result?.ResponseMessage ?? "Unable to update spendwise")
            };
        }
    }
}




