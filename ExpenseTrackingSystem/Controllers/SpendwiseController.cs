using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.SpendwiseService;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SpendwiseEntity = SpendwiseSystem.Domain.Entities.Spendwise;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SpendwiseController : ControllerBase
    {
        private readonly ISpendwiseService _spendwiseService;

        public SpendwiseController(ISpendwiseService spendwiseService)
        {
            _spendwiseService = spendwiseService;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> CreateSpendwise([FromBody] CreateSpendwiseDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var createdBy = GetCurrentUserName();
            var response = await _spendwiseService.AddSpendwise(dto, userId, createdBy);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<SpendwiseEntity>>>> GetAllSpendwises()
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var response = await _spendwiseService.GetAllSpendwises(userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<SpendwiseEntity>>> GetSpendwiseById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var response = await _spendwiseService.GetSpendwiseById(id, userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateSpendwise(Guid id, [FromBody] UpdateSpendwiseDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var modifiedBy = GetCurrentUserName();
            var response = await _spendwiseService.UpdateSpendwise(id, dto, userId, modifiedBy);
            return StatusCode(response.StatusCode, response);
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





