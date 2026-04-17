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

        public async Task<ApiResponse<BusinessDto>> CreateBusiness(BusinessDto business, string CreatedBy)
        {
            var uploadedFileName = await FileUploadHelper.FileUploadHelper.UploadFileAsync(business.File);
            business.FileName = uploadedFileName;

            try
            {

                var dbResult = await _businessRepository.CreateBusiness(business, CreatedBy);

                if (dbResult == null)
                {
                    return new ApiResponse<BusinessDto>
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.InternalServerError,
                        Message = "No response from database.",
                        Data = null
                    };
                }

                if (!dbResult.Success)
                {
                    return new ApiResponse<BusinessDto>
                    {
                        Success = false,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Message = dbResult.Message,
                        Data = null
                    };
                }

                BusinessDto data = null;
                if (!string.IsNullOrWhiteSpace(dbResult.Data))
                {
                    data = JsonSerializer.Deserialize<BusinessDto>(dbResult.Data);
                }

                return new ApiResponse<BusinessDto>
                {
                    Success = true,
                    StatusCode = (int)HttpStatusCode.Created,
                    Message = dbResult.Message,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<BusinessDto>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = ex.Message,
                    Data = null
                };
            }
        }
        public async Task<ApiResponse<BusinessDto>> DeleteBusiness(string id, string userName)
        {
            var responseFromRepo = await _businessRepository.DeleteBusiness(id, userName);

            if (responseFromRepo.Success)
                return new ApiResponse<BusinessDto>
                {
                    Success = true,
                    Message = responseFromRepo.Message,
                    StatusCode = (int)ApiResponses.Success().StatusCode,
                    Data = null
                };
            return new ApiResponse<BusinessDto>
            {
                Success = false,
                Message = responseFromRepo.Message,
                StatusCode = (int)ApiResponses.BadRequest().StatusCode,
                Data = null
            };
        }

        public async Task<ApiResponse<List<BusinessDto>>> GetAllBusiness(string UserId)
        {
            var responseFromRepo = await _businessRepository.GetAllBusiness(UserId);
            if (responseFromRepo == null)
            {
                return new ApiResponse<List<BusinessDto>>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "No response from database.",
                    Data = null
                };
            }

            if (!responseFromRepo.Success)
            {
                return new ApiResponse<List<BusinessDto>>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = responseFromRepo.Message,
                    Data = null
                };
            }

            List<BusinessDto> data = null;
            if (!string.IsNullOrWhiteSpace(responseFromRepo.Data))
            {
                data = JsonSerializer.Deserialize<List<BusinessDto>>(responseFromRepo.Data);
            }
            var fileNames = data.Select(b => b.FileName).ToList();
            //var filePath = FileUploadHelper.FileUploadHelper.GetFileUrl(fileNames, )
            return new ApiResponse<List<BusinessDto>>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = responseFromRepo.Message,
                Data = data
            };
        }
        public async Task<ApiResponse<BusinessDto>> GetBusiness(string id)
        {
            var responseFromRepo = await _businessRepository.GetBusiness(id);

            if (!responseFromRepo.Success)
                return new ApiResponse<BusinessDto>
                {
                    Success = false,
                    Message = responseFromRepo.Message,
                    StatusCode = (int)ApiResponses.BadRequest().StatusCode,
                    Data = null
                };

            return new ApiResponse<BusinessDto>
            {
                Success = true,
                Message = responseFromRepo.Message,
                StatusCode = (int)ApiResponses.Success().StatusCode,
                Data = JsonSerializer.Deserialize<BusinessDto>( responseFromRepo.Data)
            };

        }
        public async Task<ApiResponse<BusinessDto>> UpdateBusiness(BusinessDto business, string CreatedBy)
        {
            var responseFromRepo = await _businessRepository.UpdateBusiness(business, CreatedBy);

            if(!responseFromRepo.Success)
                return new ApiResponse<BusinessDto>
                {
                    Success = false,
                    Message = responseFromRepo.Message,
                    StatusCode = (int)ApiResponses.BadRequest().StatusCode,
                    Data = null
                };

            return new ApiResponse<BusinessDto>
            {
                Success = true,
                Message = responseFromRepo.Message,
                StatusCode = (int)ApiResponses.Success().StatusCode,
                Data = JsonSerializer.Deserialize<BusinessDto>(responseFromRepo.Data)
            };
        }
    }
}
