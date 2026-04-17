using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.SpendwiseService;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net;

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
        public async Task<ActionResult<ApiResponse<object>>> CreateCashBook([FromBody] CreateCashBookDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var createdBy = GetCurrentUserName();
            var response = await _cashBookService.AddCashBook(dto, userId, createdBy);
            return StatusCode(response.StatusCode, response);
        }

        
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CashBook>>>> GetAllCashBooks()
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
        public async Task<ActionResult<ApiResponse<CashBook>>> GetCashBookById(Guid id)
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
        public async Task<ActionResult<ApiResponse<CashBook>>> UpdateCashBook(Guid id, [FromBody] UpdateCashBookDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var modifiedBy = GetCurrentUserName();
            var response = await _cashBookService.UpdateCashBook(id, dto, userId, modifiedBy);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{id=guid}")]
        public async Task<ActionResult<ApiResponse<object>>> DelteCashBook(Guid id)
        {
            var responseFromService = await _cashBookService.DeleteCashBook(id);
            return StatusCode(ApiResponses.Success().StatusCode, responseFromService);
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





