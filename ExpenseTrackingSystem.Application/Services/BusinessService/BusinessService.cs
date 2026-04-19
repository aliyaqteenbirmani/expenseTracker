using System.Net;
using System.Text.Json;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.FileUploadHelper;
using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Services.BusinessService
{
    public class BusinessService : IBusinessService
    {
        private readonly IBusinessRepository _businessRepository;

        public BusinessService(IBusinessRepository businessRepository)
        {
            _businessRepository = businessRepository;
        }

        public async Task<ApiResponse<BusinessResponseDto>> CreateBusiness(BusinessDto business, string CreatedBy, string UserId)
        {
            var uploadedFileName = await FileUploadHelper.FileUploadHelper.UploadFileAsync(business.File);
            business.FileName = uploadedFileName;

            try
            {

                var dbResult = await _businessRepository.CreateBusiness(business, CreatedBy, UserId);

                if (dbResult == null)
                {
                    return new ApiResponse<BusinessResponseDto>
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.InternalServerError,
                        Message = "No response from database.",
                        Data = null
                    };
                }

                if (!dbResult.Success)
                {
                    return new ApiResponse<BusinessResponseDto>
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = dbResult.Message,
                        Data = null
                    };
                }

                BusinessResponseDto data = null;
                if (!string.IsNullOrWhiteSpace(dbResult.Data))
                {
                    data = JsonSerializer.Deserialize<BusinessResponseDto>(dbResult.Data);
                }
                return new ApiResponse<BusinessResponseDto>
                {
                    Success = true,
                    StatusCode = (int)HttpStatusCode.Created,
                    Message = dbResult.Message,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<BusinessResponseDto>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = ex.Message,
                    Data = null
                };
            }
        }
        public async Task<ApiResponse<BusinessResponseDto>> DeleteBusiness(string id, string userName)
        {
            var responseFromRepo = await _businessRepository.DeleteBusiness(id, userName);

            if (responseFromRepo.Success)
                return new ApiResponse<BusinessResponseDto>
                {
                    Success = true,
                    Message = responseFromRepo.Message,
                    StatusCode = (int)ApiResponses.Success().StatusCode,
                    Data = null
                };
            return new ApiResponse<BusinessResponseDto>
            {
                Success = false,
                Message = responseFromRepo.Message,
                StatusCode = (int)ApiResponses.BadRequest().StatusCode,
                Data = null
            };
        }

        public async Task<ApiResponse<List<BusinessResponseDto>>> GetAllBusiness(string UserId)
        {
            var responseFromRepo = await _businessRepository.GetAllBusiness(UserId);
            if (responseFromRepo == null)
            {
                return new ApiResponse<List<BusinessResponseDto>>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "No response from database.",
                    Data = null
                };
            }

            if (!responseFromRepo.Success)
            {
                return new ApiResponse<List<BusinessResponseDto>>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = responseFromRepo.Message,
                    Data = null
                };
            }

            List<BusinessResponseDto> data = null;
            if (!string.IsNullOrWhiteSpace(responseFromRepo.Data))
            {
                data = JsonSerializer.Deserialize<List<BusinessResponseDto>>(responseFromRepo.Data);
            }
            var fileNames = data.Select(b => b.FileName).ToList();
            //var filePath = FileUploadHelper.FileUploadHelper.GetFileUrl(fileNames, )
            return new ApiResponse<List<BusinessResponseDto>>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = responseFromRepo.Message,
                Data = data
            };
        }
        public async Task<ApiResponse<BusinessResponseDto>> GetBusiness(string id)
        {
            var responseFromRepo = await _businessRepository.GetBusiness(id);

            if (!responseFromRepo.Success)
                return new ApiResponse<BusinessResponseDto>
                {
                    Success = false,
                    Message = responseFromRepo.Message,
                    StatusCode = (int)ApiResponses.BadRequest().StatusCode,
                    Data = null
                };

            return new ApiResponse<BusinessResponseDto>
            {
                Success = true,
                Message = responseFromRepo.Message,
                StatusCode = (int)ApiResponses.Success().StatusCode,
                Data = JsonSerializer.Deserialize<BusinessResponseDto>( responseFromRepo.Data)
            };

        }
        public async Task<ApiResponse<BusinessResponseDto>> UpdateBusiness(BusinessDto business, string CreatedBy)
        {
            var responseFromRepo = await _businessRepository.UpdateBusiness(business, CreatedBy);

            if(!responseFromRepo.Success)
                return new ApiResponse<BusinessResponseDto>
                {
                    Success = false,
                    Message = responseFromRepo.Message,
                    StatusCode = (int)ApiResponses.BadRequest().StatusCode,
                    Data = null
                };

            return new ApiResponse<BusinessResponseDto>
            {
                Success = true,
                Message = responseFromRepo.Message,
                StatusCode = (int)ApiResponses.Success().StatusCode,
                Data = JsonSerializer.Deserialize<BusinessResponseDto>(responseFromRepo.Data)
            };
        }
    }
}
