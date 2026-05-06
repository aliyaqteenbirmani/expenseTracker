using CashBookSystem.Application.Interfaces;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.FileUploadHelper;
using SpendwiseSystem.Application.Services.PermissionAccessService;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.DTOs.CashBookDtos;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Domain.Enums;
using System.Text.Json;

namespace SpendwiseSystem.Application.Services.SpendwiseService
{
    public class CashBookService : ICashBookService
    {
        private readonly ICashBookRepository _cashbookRepository;
        private readonly IPermissionAccessService _permissionAccessService;

        public CashBookService(
            ICashBookRepository cashbookRepository,
            IPermissionAccessService permissionAccessService)
        {
            _cashbookRepository = cashbookRepository;
            _permissionAccessService = permissionAccessService;
        }

        public async Task<ApiResponse<object>> AddCashBook(
            CreateCashBookDto dto,
            string userId,
            string createdBy)
        {
            var hasAccess = await _permissionAccessService.HasBusinessOrOwnerAccessAsync(
                Guid.Parse(dto.BusinessId),
                Guid.Parse(userId),
                BusinessPermission.CASHBOOK_CREATE.ToString()
            );

            if (!hasAccess)
                return ApiResponses.Forbidden("You do not have permission to create a cashbook for this business.");

            var result = await _cashbookRepository.AddCashBook(dto, userId, createdBy);

            return result?.ResponseCode switch
            {
                "200" => ApiResponses.Created<object>(result.ResponseMessage),
                "409" => ApiResponses.Conflict(result.ResponseMessage),
                _ => ApiResponses.InternalServerError(result?.ResponseMessage ?? "Unable to create cashbook")
            };
        }

        public async Task<ApiResponse<object>> DeleteCashBook(string id, string userId)
        {
            var cashBookId = Guid.Parse(id);

            var hasAccess = await _permissionAccessService.HasCashbookOrOwnerAccessAsync(
                cashBookId,
                Guid.Parse(userId),
                BusinessPermission.CASHBOOK_DELETE.ToString()
            );

            if (!hasAccess)
                return ApiResponses.Forbidden("You do not have permission to delete this cashbook.");

            var responseFromRepo = await _cashbookRepository.DeleteCashBook(cashBookId);

            if (responseFromRepo.Success)
            {
                var files = string.IsNullOrWhiteSpace(responseFromRepo.Data)
                    ? new List<FileItemDto>()
                    : JsonSerializer.Deserialize<List<FileItemDto>>(
                        responseFromRepo.Data,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<FileItemDto>();

                foreach (var file in files)
                {
                    if (!string.IsNullOrWhiteSpace(file.FileName))
                    {
                        FileUploadHelper.FileUploadHelper.DeleteUploadedFile(file.FileName);
                    }
                }

                return ApiResponses.Success(responseFromRepo.Message);
            }

            return ApiResponses.BadRequest(responseFromRepo.Message);
        }

        public async Task<ApiResponse<List<CreateCashBookDto>>> GetAllCashBook(
            string businessId,
            string userId)
        {
            var hasAccess = await _permissionAccessService.HasBusinessOrOwnerAccessAsync(
                Guid.Parse(businessId),
                Guid.Parse(userId),
                BusinessPermission.CASHBOOK_LIST_VIEW.ToString()
            );

            if (!hasAccess)
            {
                return ApiResponse<List<CreateCashBookDto>>.FailureResponse(
                    "You do not have permission to view cashbooks for this business."
                );
            }

            var responseFromRepo = await _cashbookRepository.GetAllCashBooks(businessId);

            if (!responseFromRepo.Success)
            {
                return ApiResponse<List<CreateCashBookDto>>.FailureResponse("No cashbook found");
            }

            var cashbooks = JsonSerializer.Deserialize<List<CreateCashBookDto>>(
                responseFromRepo.Data,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return ApiResponse<List<CreateCashBookDto>>.SuccessResponse(
                "Cashbooks fetched successfully",
                cashbooks ?? new List<CreateCashBookDto>()
            );
        }

        public async Task<ApiResponse<object>> GetCashBookById(Guid id, string userId)
        {
            var hasAccess = await _permissionAccessService.HasCashbookOrOwnerAccessAsync(
                id,
                Guid.Parse(userId),
                CashbookPermission.CASHBOOK_VIEW.ToString()
            );

            if (!hasAccess)
                return ApiResponses.Forbidden("You do not have permission to view this cashbook.");

            var cashBookFromRepo = await _cashbookRepository.GetCashBookById(id, userId);

            if (!cashBookFromRepo.Success)
                return ApiResponses.BadRequest(cashBookFromRepo.Message);

            var cashBook = JsonSerializer.Deserialize<object>(
                cashBookFromRepo.Data,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return ApiResponses.SuccessWithData(cashBook, "CashBook fetched successfully");
        }

        public async Task<ApiResponse<CashBook>> UpdateCashBook(
            Guid id,
            UpdateCashBookDto dto,
            string userId,
            string modifiedBy)
        {
            var hasAccess = await _permissionAccessService.HasCashbookOrOwnerAccessAsync(
                id,
                Guid.Parse(userId),
                BusinessPermission.CASHBOOK_UPDATE.ToString()
            );

            if (!hasAccess)
            {
                return new ApiResponse<CashBook>
                {
                    StatusCode = 403,
                    Success = false,
                    Message = "You do not have permission to update this cashbook.",
                    ErrorCode = "FORBIDDEN",
                    Data = null
                };
            }

            var result = await _cashbookRepository.UpdateCashBook(
                id,
                dto.CashBook,
                userId,
                modifiedBy
            );

            if (!result.Success)
            {
                return new ApiResponse<CashBook>
                {
                    StatusCode = result.ResponseCode,
                    Success = false,
                    Message = result.Message,
                    ErrorCode = result.ResponseCode.ToString(),
                    Data = null
                };
            }

            var cashBook = JsonSerializer.Deserialize<CashBook>(
                result.Data,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return ApiResponses.SuccessWithData(cashBook, "CashBook updated successfully");
        }
    }
}