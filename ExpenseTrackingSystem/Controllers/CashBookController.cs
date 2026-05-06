using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.CurrentUserService;
using SpendwiseSystem.Application.Services.SpendwiseService;
using SpendwiseSystem.Domain.DTOs.CashBookDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CashBookController : ControllerBase
    {
        private readonly ICashBookService _cashBookService;
        private readonly ICurrentUserService _currentUserService;

        public CashBookController(ICashBookService cashBookService, ICurrentUserService currentUserService)
        {
            _cashBookService = cashBookService;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        public async Task<ActionResult<CreateCashBookDto>> CreateCashBook([FromBody] CreateCashBookDto dto)
        {
            var userId = _currentUserService.UserId.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var createdBy = _currentUserService.UserName;
            var response = await _cashBookService.AddCashBook(dto, userId, createdBy);

            if (!response.Success)
                return Conflict(ApiResponses.Conflict());

            // Fix: Cast or extract the correct type for SuccessWithData<T>
            return Ok(ApiResponses.SuccessWithData(response.Data, "Cash book created successfully"));
        }



        [HttpGet("getall")]
        public async Task<ActionResult<List<CreateCashBookDto>>> GetAllCashBooks(string id)
        {
            var userId = _currentUserService.UserId.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var response = await _cashBookService.GetAllCashBook(id, userId);

            return Ok(ApiResponses.SuccessWithData(response.Data, response.Message));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CreateCashBookDto>> GetCashBookById(Guid id)
        {
            var userId = _currentUserService.UserId.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var response = await _cashBookService.GetCashBookById(id, userId);

            return response.StatusCode switch
            {
                404 => NotFound(ApiResponses.NotFound()),
                400 => BadRequest(ApiResponses.BadRequest(response.Message)),
                401 => Unauthorized(ApiResponses.Unauthorized(response.Message)),
                200 => Ok(ApiResponses.SuccessWithData(response.Data, response.Message)),
                _ => StatusCode(response.StatusCode, ApiResponses.InternalServerError(response.Message))
            };
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<object>> UpdateCashBook(Guid id, [FromBody] UpdateCashBookDto dto)
        {
            var userId = _currentUserService.UserId.ToString();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponses.Unauthorized());

            var modifiedBy = _currentUserService.UserName;
            var response = await _cashBookService.UpdateCashBook(id, dto, userId, modifiedBy);

            if(!response.Success)
                return BadRequest(ApiResponses.BadRequest(response.Message));

            return Ok(ApiResponses.SuccessWithData(response.Data, response.Message));
        }

        [HttpDelete("{id=guid}")]
        public async Task<ActionResult<CreateCashBookDto>> DelteCashBook(string id)
        {
            var userId = _currentUserService.UserId.ToString();
            if(string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponses.Unauthorized());

            var responseFromService = await _cashBookService.DeleteCashBook(id, userId);

            if (!responseFromService.Success)
                return BadRequest(ApiResponses.BadRequest());

            return Ok(ApiResponses.Success(responseFromService.Message));
        }
    }
}



