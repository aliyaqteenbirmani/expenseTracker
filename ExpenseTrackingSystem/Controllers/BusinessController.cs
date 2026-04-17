using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.BusinessService;
using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.Entities;
using System.Security.Claims;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessService _businessService;

        public BusinessController(IBusinessService businessService)
        {
            _businessService = businessService;
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<BusinessDto>>> CreateBusiness([FromForm] BusinessDto business)
        {
            var CreatedBy = GetCurrentUserName();
            var result = await _businessService.CreateBusiness(business, CreatedBy);
            if (!result.Success)
                return new ApiResponse<BusinessDto>
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = result.Message,
                    ErrorCode = "BUSINESS_CREATION_FAILED",
                    Data = null
                };

            return new ApiResponse<BusinessDto>
            {
                StatusCode = StatusCodes.Status201Created,
                Success = true,
                Message = result.Message,
                ErrorCode = null,
                Data = result.Data
            };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBusiness(string id)
        {
            var result = await _businessService.GetBusiness(id);
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<BusinessDto>>> GetAllBusinesses()
        {
            var UserId = GetCurrentUserId();
            
            if(UserId == null)
                return Unauthorized(ApiResponses.Unauthorized());

            var result = await _businessService.GetAllBusiness(UserId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<BusinessDto>>> DeleteBusiness(string id)
        {
            var userName = GetCurrentUserName();
            var result = await _businessService.DeleteBusiness(id, userName);

            if (!result.Success)
                return BadRequest(result);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<BusinessDto>>> UpdateBusiness([FromForm] BusinessDto business)
        {
            var userName = GetCurrentUserName();
            var result = await _businessService.UpdateBusiness(business, userName);
            if (!result.Success)
                return BadRequest(result);
            return StatusCode(result.StatusCode, result);
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