using ExpenseTrackingSystem.Application.Interfaces;
using ExpenseTrackingSystem.Application.Helper;
using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;

namespace ExpenseTrackingSystem.Application.Services.CashBookService
{
    public class CashBookService : ICashBookService
    {
        private readonly ICashBookRepository _cashBookRepository;

        public CashBookService(ICashBookRepository cashBookRepository)
        {
            _cashBookRepository = cashBookRepository;
        }

        public async Task<ApiResponse<object>> AddCashBook(CreateCashBookDto dto, string userId, string createdBy)
        {
            var result = await _cashBookRepository.AddCashBook(dto.BookName, userId, createdBy);

            return result?.ResponseCode switch
            {
                "200" => ApiResponses.Success(result.ResponseMessage),
                "409" => ApiResponses.Conflict(result.ResponseMessage),
                _ => ApiResponses.InternalServerError(result?.ResponseMessage ?? "Unable to create cash book")
            };
        }

        public async Task<ApiResponse<List<CashBook>>> GetAllCashBooks(string userId)
        {
            var cashBooks = await _cashBookRepository.GetAllCashBooks(userId);
            return ApiResponses.SuccessWithData(cashBooks ?? new List<CashBook>(), "Cash books fetched successfully");
        }

        public async Task<ApiResponse<CashBook>> GetCashBookById(Guid id, string userId)
        {
            var cashBook = await _cashBookRepository.GetCashBookById(id, userId);

            if (cashBook is null)
            {
                return new ApiResponse<CashBook>
                {
                    StatusCode = 404,
                    Success = false,
                    Message = "Cash book not found",
                    ErrorCode = "NOT_FOUND",
                    Data = null
                };
            }

            return ApiResponses.SuccessWithData(cashBook, "Cash book fetched successfully");
        }

        public async Task<ApiResponse<object>> UpdateCashBook(Guid id, UpdateCashBookDto dto, string userId, string modifiedBy)
        {
            var result = await _cashBookRepository.UpdateCashBook(id, dto.BookName, userId, modifiedBy);

            return result?.ResponseCode switch
            {
                "200" => ApiResponses.Success(result.ResponseMessage),
                "404" => ApiResponses.NotFound(result.ResponseMessage),
                "409" => ApiResponses.Conflict(result.ResponseMessage),
                _ => ApiResponses.InternalServerError(result?.ResponseMessage ?? "Unable to update cash book")
            };
        }
    }
}
