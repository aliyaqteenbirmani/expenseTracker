using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
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
        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("AddCashTransaction")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<CashTransaction>>> AddCashTransaction([FromForm] CashTransactionDto transactionDto)
        {
            if (!ModelState.IsValid)
            {
                {
                    return BadRequest(new ApiResponse<CashTransactionDto>
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Success = false,
                        Message = "Invalid transaction data.",
                        Data = null
                    });
                }
            }

            try
            {
                if (transactionDto.File != null && transactionDto.File.Length > 0)
                {
                    var uploadedFileName = await FileUploadHelper.UploadFileAsync(transactionDto.File);
                    transactionDto.FileName = uploadedFileName;
                }

                var createdBy = GetCurrentUserName();
                var resultFromService = await _transactionService.AddCashTransaction(transactionDto, createdBy);

                if (resultFromService.Success)
                {
                    return Ok(new ApiResponse<CashTransaction>
                    {
                        StatusCode = (int)HttpStatusCode.Created,
                        Success = true,
                        Message = resultFromService.Message,
                        Data = resultFromService.Data
                    });
                }

                if (!string.IsNullOrWhiteSpace(resultFromService.Data?.FileName))
                {
                    FileUploadHelper.DeleteUploadedFile(resultFromService.Data.FileName);
                }

                return BadRequest(new ApiResponse<CashTransaction>
                {
                    StatusCode = resultFromService.StatusCode,
                    Success = resultFromService.Success,
                    Message = resultFromService.Message,
                    ErrorCode = resultFromService.ErrorCode,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(transactionDto.FileName))
                {
                    FileUploadHelper.DeleteUploadedFile(transactionDto.FileName);
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<CashTransactionDto>
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Internal error occurred: {ex.Message}",
                    Success = false,
                    Data = null
                });
            }

        }

        [HttpGet("getall")]
        public async Task<ActionResult<ApiResponse<List<AllCashTransactionDto>>>> GetAllCashTransactions(string CashBookId)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            try
            {
                var responseFromService = await _transactionService.GetAllTransactionsOfCashBook(CashBookId);

                if (responseFromService.Success)
                {
                    List<string> fileNames = responseFromService.Data.Select(x => x.FileName).ToList();
                    var filesPath = FileUploadHelper.GetFileUrl(fileNames, Request);
                    for (int i = 0; i < responseFromService.Data.Count; i++)
                    {
                        responseFromService.Data[i].FilePath = filesPath[i];
                    }
                    return Ok(new ApiResponse<List<AllCashTransactionDto>>
                    {
                        StatusCode = ApiResponses.Success().StatusCode,
                        Success = true,
                        Message = responseFromService.Message,
                        Data = responseFromService.Data
                    });
                }
                return BadRequest(new ApiResponse<List<AllCashTransactionDto>>
                {
                    StatusCode = ApiResponses.BadRequest().StatusCode,
                    Success = false,
                    Message = responseFromService.Message,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<AllCashTransactionDto>>
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Internal error occurred: {ex.Message}",
                    Success = false,
                    Data = null
                });
            }
            
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ApiResponseRaw>>> DeleteCashTransaction(Guid id)
        {
            var modifiedBy = GetCurrentUserName();
            try
            {
                var responseFromService = await _transactionService.DeleteTransaction(id, modifiedBy);

                if (responseFromService.Success)
                {
                    return Ok(new ApiResponse<ApiResponseRaw>
                    {
                        StatusCode = ApiResponses.Success().StatusCode,
                        Success = true,
                        Message = responseFromService.Message
                    });
                }

                return BadRequest(new ApiResponse<ApiResponseRaw>
                {
                    StatusCode = ApiResponses.BadRequest().StatusCode,
                    Success = false,
                    Message = responseFromService.Message,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<ApiResponseRaw>
                {
                    StatusCode = ApiResponses.InternalServerError().StatusCode,
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<CashTransactionDto>>> GetCashTransactionById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            try
            {
                var responseFromService = await _transactionService.GetTransactionById(id);

                if (responseFromService.Success)
                {
                    return Ok(new ApiResponse<CashTransactionDto>
                    {
                        StatusCode = ApiResponses.Success().StatusCode,
                        Success = true,
                        Message = responseFromService.Message,
                        Data = responseFromService.Data
                    });
                }
                return BadRequest(new ApiResponse<CashTransactionDto>
                {
                    StatusCode = ApiResponses.BadRequest().StatusCode,
                    Success = false,
                    Message = responseFromService.Message,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<CashTransactionDto>
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Internal error occurred: {ex.Message}",
                    Success = false,
                    Data = null
                });
            }
        }

        [HttpPut("update")]
        public async Task<ActionResult<ApiResponse<CTUpdateResponseDto>>> UpdateCashTransaction([FromForm] CashTransactionUpdateDto transactionUpdateDto)
        {
            /*if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<CashTransactionDto>
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Invalid transaction data.",
                    Data = null
                });
            }*/

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

                var modifiedBy = GetCurrentUserName();
                var resultFromService = await _transactionService.UpdateCashTransaction(transactionUpdateDto, modifiedBy);

                if (resultFromService.Success)
                {
                    return Ok(new ApiResponse<CTUpdateResponseDto>
                    {
                        StatusCode = ApiResponses.Success().StatusCode,
                        Success = true,
                        Message = resultFromService.Message,
                        Data = resultFromService.Data
                    });
                }

                if (!string.IsNullOrWhiteSpace(resultFromService.Data?.FileName))
                {
                    FileUploadHelper.DeleteUploadedFile(resultFromService.Data.FileName);
                }

                return BadRequest(new ApiResponse<CTUpdateResponseDto>
                {
                    StatusCode = resultFromService.StatusCode,
                    Success = resultFromService.Success,
                    Message = resultFromService.Message,
                    ErrorCode = resultFromService.ErrorCode,
                    Data = null
                });
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(transactionUpdateDto.FileName))
                {
                    FileUploadHelper.DeleteUploadedFile(transactionUpdateDto.FileName);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<CashTransactionDto>
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Internal error occurred: {ex.Message}",
                    Success = false,
                    Data = null
                });
            }
        }


        [HttpGet("getfile")]
        public async Task<ActionResult<ApiResponse<CashTransactionFileDto>>> GetCashTransactionFile(string id)
        {
            try
            {
                var responseFromService = await _transactionService.GetCashTransactionFile(id);

                if (!responseFromService.Success)
                    return BadRequest(new ApiResponse<CashTransactionFileDto>
                    {
                        Success = false,
                        StatusCode = ApiResponses.BadRequest().StatusCode,
                        Message = responseFromService.Message,
                        Data = null
                    });

                var filePath = FileUploadHelper.GetFileUrl(new List<string?> { responseFromService.Data.FileName }, Request).FirstOrDefault();
                responseFromService.Data.FilePath = filePath;

                return Ok(new ApiResponse<CashTransactionFileDto>
                {
                    Success = true,
                    StatusCode = ApiResponses.Success().StatusCode,
                    Message = responseFromService.Message,
                    Data = responseFromService.Data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<CashTransactionFileDto>
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Internal error occurred: {ex.Message}",
                    Success = false,
                    Data = null
                });
            }

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
