using CashBookSystem.Application.Interfaces;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.FileUploadHelper;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;
using System.Text.Json;


namespace SpendwiseSystem.Application.Services.SpendwiseService
{
    public class CashBookService : ICashBookService
    {
        private readonly ICashBookRepository _cashbookRepository;

        public CashBookService(ICashBookRepository cashbookRepository)
        {
            _cashbookRepository = cashbookRepository;
        }

        public async Task<ApiResponse<object>> AddCashBook(CreateCashBookDto dto, string userId, string createdBy)
        {
            var result = await _cashbookRepository.AddCashBook(dto, userId, createdBy);

            return result?.ResponseCode switch
            {
                "200" => ApiResponses.Success(result.ResponseMessage),
                "409" => ApiResponses.Conflict(result.ResponseMessage),
                _ => ApiResponses.InternalServerError(result?.ResponseMessage ?? "Unable to create cashbook")
            };
        }

        public async Task<ApiResponse<object>> DeleteCashBook(Guid id)
        {
            var responseFromRepo = await _cashbookRepository.DeleteCashBook(id);
            
            if(responseFromRepo.Success)
            {
                var files = string.IsNullOrWhiteSpace(responseFromRepo.Data)
                    ? new List<FileItemDto>()
                    : JsonSerializer.Deserialize<List<FileItemDto>>(
                        responseFromRepo.Data,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                foreach (var fileName in files)
                {
                    FileUploadHelper.FileUploadHelper.DeleteUploadedFile(fileName.FileName.ToString());
                }

                return new ApiResponse<object>
                {
                    Success = true,
                    StatusCode = ApiResponses.Success().StatusCode,
                    Message = responseFromRepo.Message
                };
            }
            return new ApiResponse<object>
            {
                Success = false,
                Message = responseFromRepo.Message
            };
        }

        public async Task<ApiResponse<List<CashBook>>> GetAllCashBook(string userId)
        {
            var spendwiseItems = await _cashbookRepository.GetAllCashBooks(userId);

            if(spendwiseItems == null || string.IsNullOrWhiteSpace(spendwiseItems.Data))
            {
                return ApiResponses.SuccessWithData(new List<CashBook>(), "No cashbooks found");
            }

            var cashbooks = System.Text.Json.JsonSerializer.Deserialize<List<CashBook>>(spendwiseItems.Data, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            return ApiResponses.SuccessWithData(cashbooks ?? new List<CashBook>(), "Cashbooks fetched successfully");
        }

        public async Task<ApiResponse<CashBook>> GetCashBookById(Guid id, string userId)
        {
            var cashBookFromRepo = await _cashbookRepository.GetCashBookById(id, userId);

            if (!cashBookFromRepo.Success)
            {
                return new ApiResponse<CashBook>
                {
                    StatusCode = 404,
                    Success = false,
                    Message = "CashBook not found",
                    ErrorCode = "NOT_FOUND",
                    Data = null
                };
            }

            var cashBook = System.Text.Json.JsonSerializer.Deserialize<CashBook>(cashBookFromRepo.Data, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            return ApiResponses.SuccessWithData(cashBook, "CashBook fetched successfully");
        }

        public async Task<ApiResponse<CashBook>> UpdateCashBook(Guid id, UpdateCashBookDto dto, string userId, string modifiedBy)
        {
            var result = await _cashbookRepository.UpdateCashBook(id, dto.CashBook, userId, modifiedBy);
            
            if(!result.Success)
            {
                return new ApiResponse<CashBook>
                {
                    StatusCode = result.ResponseCode,
                    Success = false,
                    Message = result.Message,
                    ErrorCode = result.ResponseCode.ToString(),
                    Data = null
                };
                //return ApiResponse<CashBook> result?.ResponseCode switch
                //{
                //    200 => ApiResponses.Success(result.Message),
                //    404 => ApiResponses.NotFound(result.Message),
                //    409 => ApiResponses.Conflict(result.Message),
                //    _ => ApiResponses.InternalServerError(result?.Message ?? "Unable to update cashbook")
                //};
            }

            var cashBook = System.Text.Json.JsonSerializer.Deserialize<CashBook>(result.Data, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            return ApiResponses.SuccessWithData(cashBook, "CashBook updated successfully");
        }
    }
}




