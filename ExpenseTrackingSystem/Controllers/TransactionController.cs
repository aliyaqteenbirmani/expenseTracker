using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.CurrentUserService;
using SpendwiseSystem.Application.Services.FileUploadHelper;
using SpendwiseSystem.Application.Services.TransactionService;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.CashTransactionDtos;
using SpendwiseSystem.Domain.Entities;
using System.Net;
using System.Security.Claims;

namespace SpendwiseSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ICurrentUserService _currentUserService;
        public TransactionController(ITransactionService transactionService, ICurrentUserService currentUserService)
        {
            _transactionService = transactionService;
            _currentUserService = currentUserService;
        }

        [HttpPost("AddCashTransaction")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<CashTransaction>> AddCashTransaction([FromForm] CashTransactionDto transactionDto)
        {
            if (!ModelState.IsValid)
            {
                {
                    return BadRequest(ApiResponses.BadRequest());
                }
            }

            try
            {
                if (transactionDto.File != null && transactionDto.File.Length > 0)
                {
                    var uploadedFileName = await FileUploadHelper.UploadFileAsync(transactionDto.File);
                    transactionDto.FileName = uploadedFileName;
                }

                var createdBy = _currentUserService.UserName;
                var userId = _currentUserService.UserId;

                if(userId == Guid.Empty)
                    return Unauthorized(ApiResponses.Unauthorized());

                var resultFromService = await _transactionService.AddCashTransaction(transactionDto, createdBy, userId ?? Guid.Empty);

                if (resultFromService.Success)
                {
                    return Ok(ApiResponses.Success(resultFromService.Message));
                }

                if (!string.IsNullOrWhiteSpace(resultFromService.Data?.FileName))
                {
                    FileUploadHelper.DeleteUploadedFile(resultFromService.Data.FileName);
                }

                return BadRequest(ApiResponses.BadRequest());
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(transactionDto.FileName))
                {
                    FileUploadHelper.DeleteUploadedFile(transactionDto.FileName);
                }

                return StatusCode(ApiResponses.InternalServerError().StatusCode, ex.Message);
            }

        }

        [HttpGet("getall")]
        public async Task<ActionResult<List<AllCashTransactionDto>>> GetAllCashTransactions(string CashBookId)
        {
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            try
            {
                var responseFromService = await _transactionService.GetAllTransactionsOfCashBook(CashBookId, userId ?? Guid.Empty);

                if (responseFromService.Success)
                {
                    List<string> fileNames = responseFromService.Data.Select(x => x.FileName).ToList();
                    var filesPath = FileUploadHelper.GetFileUrl(fileNames, Request);
                    for (int i = 0; i < responseFromService.Data.Count; i++)
                    {
                        responseFromService.Data[i].FilePath = filesPath[i];
                    }
                    return Ok(ApiResponses.SuccessWithData(responseFromService.Data, responseFromService.Message));
                }

                return BadRequest(ApiResponses.BadRequest());
            }
            catch (Exception ex)
            {
                return StatusCode(ApiResponses.InternalServerError().StatusCode, ex.Message);
            }
            
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponseRaw>> DeleteCashTransaction(Guid id)
        {
            var modifiedBy = _currentUserService.UserName;
            var userId = _currentUserService.UserId;
            if(userId == Guid.Empty)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            try
            {
                var responseFromService = await _transactionService.DeleteTransaction(id, modifiedBy, userId ?? Guid.Empty);

                if (responseFromService.Success)
                {
                    return Ok(ApiResponses.Success(responseFromService.Message));
                }

                return BadRequest(ApiResponses.BadRequest());
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CashTransactionDto>> GetCashTransactionById(Guid id)
        {
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            try
            {
                var responseFromService = await _transactionService.GetTransactionById(id,userId ?? Guid.Empty);

                if (responseFromService.Success)
                {
                    return Ok(ApiResponses.SuccessWithData(responseFromService.Data, responseFromService.Message));
                }
                return BadRequest(ApiResponses.BadRequest());
            }
            catch (Exception ex)
            {
                return StatusCode(ApiResponses.InternalServerError().StatusCode, ex.Message);
            }
        }

        [HttpPut("update")]
        public async Task<ActionResult<CTUpdateResponseDto>> UpdateCashTransaction([FromForm] CashTransactionUpdateDto transactionUpdateDto)
        {
            var userId = _currentUserService.UserId;
            var userName = _currentUserService.UserName;
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            try
            {
                if (transactionUpdateDto.File != null && transactionUpdateDto.File.Length > 0)
                {
                    if(!FileUploadHelper.IsFileExists(transactionUpdateDto.File.FileName))
                    {
                        var uploadedFileName = await FileUploadHelper.UploadFileAsync(transactionUpdateDto.File);
                        transactionUpdateDto.FileName = uploadedFileName;

                    }
                }

                var resultFromService = await _transactionService.UpdateCashTransaction(transactionUpdateDto, userName, userId ?? Guid.Empty);

                if (resultFromService.Success)
                {
                    return Ok(ApiResponses.SuccessWithData(resultFromService.Data, resultFromService.Message));
                }

                if (!string.IsNullOrWhiteSpace(resultFromService.Data?.FileName))
                {
                    FileUploadHelper.DeleteUploadedFile(resultFromService.Data.FileName);
                }

                return BadRequest(ApiResponses.BadRequest());
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(transactionUpdateDto.FileName))
                {
                    FileUploadHelper.DeleteUploadedFile(transactionUpdateDto.FileName);
                }
                return StatusCode(ApiResponses.InternalServerError().StatusCode, ex.Message);
            }
        }


        [HttpGet("getfile")]
        public async Task<ActionResult<ApiResponse<CashTransactionFileDto>>> GetCashTransactionFile(string id)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var responseFromService = await _transactionService.GetCashTransactionFile(id, userId ?? Guid.Empty);

                if (!responseFromService.Success)
                    return BadRequest(ApiResponses.BadRequest());

                var filePath = FileUploadHelper.GetFileUrl(new List<string?> { responseFromService.Data.FileName }, Request).FirstOrDefault();
                responseFromService.Data.FilePath = filePath;

                return Ok(ApiResponses.SuccessWithData(responseFromService.Data, responseFromService.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(ApiResponses.InternalServerError().StatusCode, ex.Message);
            }

        }
    }
}
