using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.BusinessService;
using SpendwiseSystem.Application.Services.FileUploadHelper;
using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.Entities;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessService _businessService;
        private readonly IWebHostEnvironment _environment;

        public BusinessController(IBusinessService businessService, IWebHostEnvironment environment)
        {
            _businessService = businessService;
            _environment = environment;
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<BusinessResponseDto>>> CreateBusiness([FromForm] BusinessDto business)
        {
            var CreatedBy = GetCurrentUserName();
            var userId = GetCurrentUserId();
            //business.FileName = await FileUploadHelper.UploadFileAsync(business.File);

            var result = await _businessService.CreateBusiness(business, CreatedBy, userId);

            if (!result.Success)
                return Conflict(new ApiResponse<BusinessResponseDto>
                {
                    StatusCode = StatusCodes.Status409Conflict,
                    Success = false,
                    Message = result.Message,
                    ErrorCode = "BUSINESS_CREATION_FAILED",
                    Data = null
                });

            var fileName = result.Data.FileName;
            var fileUrl = FileUploadHelper.GetFileUrl(new List<string> { fileName },Request);
            result.Data.FileUrl = fileUrl.FirstOrDefault();
            return CreatedAtAction(nameof(GetBusiness), new { id = result.Data.Id }, new ApiResponse<BusinessResponseDto>
            {
                StatusCode = StatusCodes.Status201Created,
                Success = true,
                Message = result.Message,
                ErrorCode = null,
                Data = result.Data
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBusiness(string id)
        {
            var result = await _businessService.GetBusiness(id);
            if (!result.Success)
                return BadRequest(new ApiResponse<BusinessResponseDto>
                    {
                        Success = false,
                        StatusCode = ApiResponses.BadRequest().StatusCode,
                        Message = ApiResponses.BadRequest().Message,
                        ErrorCode = ApiResponses.BadRequest().ErrorCode,
                        Data = null
                    });

            return Ok(new ApiResponse<BusinessResponseDto>
                    {
                        Success = false,
                        StatusCode = ApiResponses.Success().StatusCode,
                        Message = ApiResponses.Success().Message,
                        ErrorCode = ApiResponses.Success().ErrorCode,
                        Data = result.Data
                    });
        }

        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<List<BusinessResponseDto>>>> GetAllBusinesses()
        {
            var UserId = GetCurrentUserId();

            if (UserId == null)
                return Unauthorized(
                    new ApiResponse<List<BusinessResponseDto>>
                    {
                        Success = false,
                        StatusCode = ApiResponses.Unauthorized().StatusCode,
                        Message = ApiResponses.Unauthorized().Message,
                        ErrorCode = ApiResponses.Unauthorized().ErrorCode,
                        Data = null
                    });

            try
            {
                var result = await _businessService.GetAllBusiness(UserId);
                for (int i = 0; i < result.Data.Count; i++)
                {
                    var fileName = result.Data[i].FileName;

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        result.Data[i].FileUrl = FileUploadHelper.GetFileUrl(new List<string> { fileName }, Request).FirstOrDefault();
                    }
                    else
                    {
                        result.Data[i].FileUrl = null;
                    }
                }
                if (!result.Success)
                    return BadRequest(
                    new ApiResponse<List<BusinessResponseDto>>
                    {
                        Success = false,
                        StatusCode = ApiResponses.BadRequest().StatusCode,
                        Message = ApiResponses.BadRequest().Message,
                        ErrorCode = ApiResponses.BadRequest().ErrorCode,
                        Data = null
                    });

                return Ok(
                    new ApiResponse<List<BusinessResponseDto>>
                    {
                        Success = true,
                        StatusCode = ApiResponses.Success().StatusCode,
                        Message = ApiResponses.Success().Message,
                        ErrorCode = ApiResponses.Success().ErrorCode,
                        Data = result.Data
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponses.InternalServerError(ex.Message));
            }
        }

        [HttpPatch]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<BusinessResponseDto>>> UpdateBusiness([FromForm] BusinessDto business)
        {
            var userName = GetCurrentUserName();
            if (business.File != null && business.File.Length > 0)
            {
                if (!FileUploadHelper.IsFileExists(business.File.FileName))
                {
                    var uploadedFileName = await FileUploadHelper.UploadFileAsync(business.File);
                    business.FileName = uploadedFileName;
                }
            }

            var result = await _businessService.UpdateBusiness(business, userName);

            if (!result.Success)
                return BadRequest(
                    new ApiResponse<BusinessResponseDto>
                    {
                        Success = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = result.Message,
                        ErrorCode = "BUSINESS_UPDATE_FAILED",
                        Data = null
                    });
            return Ok(
                new ApiResponse<BusinessResponseDto>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = result.Message,
                    ErrorCode = null,
                    Data = result.Data
                });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<BusinessResponseDto>>> DeleteBusiness(string id)
        {
            var userName = GetCurrentUserName();
            var result = await _businessService.DeleteBusiness(id, userName);

            if (!result.Success)
                return BadRequest(new ApiResponse<BusinessResponseDto>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = result.Message,
                    ErrorCode = "BUSINESS_DELETION_FAILED",
                    Data = null
                });

            return Ok(new ApiResponse<BusinessResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = result.Message,
                ErrorCode = null,
                Data = result.Data
            });
        }


        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private string GetCurrentUserName()
        {
            return User.FindFirstValue(ClaimTypes.Name) ?? "System";
        }
    }
}