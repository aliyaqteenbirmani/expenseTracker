using Azure;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.SpendwiseService;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.Entities;
using System.Net;
using System.Security.Claims;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CashBookController : ControllerBase
    {
        private readonly ICashBookService _cashBookService;

        public CashBookController(ICashBookService cashBookService)
        {
            _cashBookService = cashBookService;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CreateCashBookDto>>> CreateCashBook([FromBody] CreateCashBookDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(new ApiResponse<CreateCashBookDto>
                {
                    Success = false,
                    StatusCode = ApiResponses.Unauthorized().StatusCode,
                    Message = ApiResponses.Unauthorized().Message,
                    ErrorCode = ApiResponses.Unauthorized().ErrorCode,
                    Data = null
                });
            }

            var createdBy = GetCurrentUserName();
            var response = await _cashBookService.AddCashBook(dto, userId, createdBy);

            if (!response.Success)
                return Conflict(new ApiResponse<CreateCashBookDto>
                {
                    Success = false,
                    StatusCode = ApiResponses.Conflict().StatusCode,
                    Message = ApiResponses.Conflict().Message,
                    ErrorCode = ApiResponses.Conflict().ErrorCode,
                    Data = null
                });

            return Ok(new ApiResponse<CreateCashBookDto>
            {
                Success = true,
                StatusCode = ApiResponses.Success().StatusCode,
                Message = ApiResponses.Success().Message,
                ErrorCode = ApiResponses.Success().ErrorCode,
                Data = null
            });
        }



            [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CreateCashBookDto>>>> GetAllCashBooks()
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var response = await _cashBookService.GetAllCashBook(userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<CreateCashBookDto>>> GetCashBookById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var response = await _cashBookService.GetCashBookById(id, userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateCashBook(Guid id, [FromBody] UpdateCashBookDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized(new ApiResponse<UpdateCashBookDto>
                {
                    Success = true,
                    StatusCode = ApiResponses.Unauthorized().StatusCode,
                    Message = ApiResponses.Unauthorized().Message,
                    ErrorCode = ApiResponses.Unauthorized().ErrorCode,
                    Data = null
                });

            var modifiedBy = GetCurrentUserName();
            var response = await _cashBookService.UpdateCashBook(id, dto, userId, modifiedBy);
            if(!response.Success)
                return BadRequest(new ApiResponse<UpdateCashBookDto>
                {
                    Success = false,
                    StatusCode = ApiResponses.BadRequest().StatusCode,
                    Message = ApiResponses.BadRequest().Message,
                    ErrorCode = ApiResponses.BadRequest().ErrorCode,
                    Data = dto
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = ApiResponses.Success().StatusCode,
                Message = ApiResponses.Success().Message,
                ErrorCode = ApiResponses.Success().ErrorCode,
                Data = response
            });
        }

        [HttpDelete("{id=guid}")]
        public async Task<ActionResult<ApiResponse<CreateCashBookDto>>> DelteCashBook(Guid id)
        {
            var responseFromService = await _cashBookService.DeleteCashBook(id);

            if (!responseFromService.Success)
                return BadRequest(new ApiResponse<UpdateCashBookDto>
                {
                    Success = false,
                    StatusCode = ApiResponses.BadRequest().StatusCode,
                    Message = ApiResponses.BadRequest().Message,
                    ErrorCode = ApiResponses.BadRequest().ErrorCode,
                    Data = null
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = ApiResponses.Success().StatusCode,
                Message = ApiResponses.Success().Message,
                ErrorCode = ApiResponses.Success().ErrorCode,
                Data = responseFromService.Data
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





