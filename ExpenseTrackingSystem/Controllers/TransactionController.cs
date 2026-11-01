using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.CurrentUserService;
using SpendwiseSystem.Application.Services.FileUploadHelper;
using SpendwiseSystem.Application.Services.PermissionAccessService;
using SpendwiseSystem.Application.Services.TransactionService;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.ReceiptOcrService;
using SpendwiseSystem.Application.Services.ReceiptParsingService;
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
        private readonly IPermissionAccessService _permissionAccessService;
        private readonly IReceiptDraftRepository _receiptDraftRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IReceiptOcrService _receiptOcrService;
        private readonly IReceiptParsingService _receiptParsingService;

        public TransactionController(
            ITransactionService transactionService,
            ICurrentUserService currentUserService,
            IPermissionAccessService permissionAccessService,
            IReceiptDraftRepository receiptDraftRepository,
            IWebHostEnvironment environment,
            IReceiptOcrService receiptOcrService,
            IReceiptParsingService receiptParsingService)
        {
            _transactionService = transactionService;
            _currentUserService = currentUserService;
            _permissionAccessService = permissionAccessService;
            _receiptDraftRepository = receiptDraftRepository;
            _environment = environment;
            _receiptOcrService = receiptOcrService;
            _receiptParsingService = receiptParsingService;
        }

        [HttpPost("receipts/drafts")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadReceiptDraft([FromForm] ReceiptDraftUploadDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue || userId.Value == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            if (request?.CashBookId == Guid.Empty || request?.File == null || request.File.Length == 0)
                return BadRequest(ApiResponses.BadRequest("A cashbook and non-empty receipt file are required."));

            if (request.File.Length > 10 * 1024 * 1024)
                return BadRequest(ApiResponses.BadRequest("Receipt files must be 10 MB or smaller."));

            var hasAccess = await _permissionAccessService.HasCashbookOrOwnerAccessAsync(
                request.CashBookId,
                userId.Value,
                SpendwiseSystem.Domain.Enums.CashbookPermission.TRANSACTION_CREATE.ToString());

            if (!hasAccess)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponses.Forbidden("You do not have permission to add transactions to this cashbook."));

            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            var contentType = GetReceiptContentType(extension);
            if (contentType == null || !await HasValidReceiptSignatureAsync(request.File, extension))
                return BadRequest(ApiResponses.BadRequest("Upload a valid JPG, PNG, or PDF receipt."));

            var now = DateTime.UtcNow;
            var draftId = Guid.NewGuid();
            var storedFileName = $"{draftId:N}{extension}";
            var storageDirectory = Path.Combine(_environment.ContentRootPath, "App_Data", "receipt-drafts");
            Directory.CreateDirectory(storageDirectory);
            var storedPath = Path.Combine(storageDirectory, storedFileName);

            try
            {
                await using (var output = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await request.File.CopyToAsync(output);

                var expiresOn = now.AddDays(1);
                await _receiptDraftRepository.AddAsync(new SpendwiseSystem.Domain.Entities.ReceiptDraft
                {
                    Id = draftId,
                    CashBookId = request.CashBookId,
                    UserId = userId.Value,
                    StoredFileName = storedFileName,
                    ContentType = contentType,
                    FileSize = request.File.Length,
                    Status = "Uploaded",
                    CreatedOn = now,
                    ExpiresOn = expiresOn
                });

                return StatusCode(StatusCodes.Status201Created, ApiResponses.Created(
                    new ReceiptDraftUploadResponseDto
                    {
                        DraftId = draftId,
                        CashBookId = request.CashBookId,
                        Status = "Uploaded",
                        ExpiresOn = expiresOn
                    },
                    "Receipt uploaded. It has not been analyzed or added as a transaction yet."));
            }
            catch
            {
                if (System.IO.File.Exists(storedPath))
                    System.IO.File.Delete(storedPath);
                throw;
            }
        }

        private static string GetReceiptContentType(string extension) => extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => null
        };

        private static async Task<bool> HasValidReceiptSignatureAsync(IFormFile file, string extension)
        {
            var header = new byte[8];
            await using var stream = file.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header.AsMemory());

            return extension switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".pdf" => bytesRead >= 5 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46 && header[4] == 0x2D,
                _ => false
            };
        }

        [HttpPost("receipts/drafts/{draftId:guid}/analyze")]
        public async Task<IActionResult> AnalyzeReceiptDraft(Guid draftId)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue || userId.Value == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var draft = await _receiptDraftRepository.GetOwnedAsync(draftId, userId.Value, DateTime.UtcNow);
            if (draft == null)
                return NotFound(ApiResponses.NotFound("Receipt draft was not found or has expired."));

            var hasAccess = await _permissionAccessService.HasCashbookOrOwnerAccessAsync(
                draft.CashBookId,
                userId.Value,
                SpendwiseSystem.Domain.Enums.CashbookPermission.TRANSACTION_CREATE.ToString());
            if (!hasAccess)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponses.Forbidden());

            var storedFileName = Path.GetFileName(draft.StoredFileName);
            if (!string.Equals(storedFileName, draft.StoredFileName, StringComparison.Ordinal))
                return NotFound(ApiResponses.NotFound("Receipt draft was not found."));

            var receiptPath = Path.Combine(_environment.ContentRootPath, "App_Data", "receipt-drafts", storedFileName);
            if (!System.IO.File.Exists(receiptPath))
                return NotFound(ApiResponses.NotFound("Receipt file was not found."));

            SpendwiseSystem.Application.Services.ReceiptOcrService.ReceiptOcrResult ocrResult;
            try
            {
                ocrResult = await _receiptOcrService.ExtractTextAsync(receiptPath, draft.ContentType);
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    ApiResponses.ServiceUnavailable("Receipt text extraction is currently unavailable."));
            }

            if (string.IsNullOrWhiteSpace(ocrResult.Text))
                return UnprocessableEntity(ApiResponses.ValidationError(
                    new { receipt = "No readable text was found. Try a clearer, well-lit image." },
                    "Receipt text could not be recognized."));

            await _receiptDraftRepository.UpdateOcrResultAsync(draftId, ocrResult.Text, ocrResult.Confidence);

            return Ok(ApiResponses.SuccessWithData(
                new ReceiptOcrAnalysisDto
                {
                    DraftId = draft.Id,
                    Status = "OcrComplete",
                    RecognizedText = ocrResult.Text,
                    Confidence = ocrResult.Confidence
                },
                "Receipt text extracted. Review it before continuing."));
        }

        [HttpPost("receipts/drafts/{draftId:guid}/suggest")]
        public async Task<IActionResult> SuggestReceiptDraft(Guid draftId)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue || userId.Value == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var draft = await _receiptDraftRepository.GetOwnedAsync(draftId, userId.Value, DateTime.UtcNow);
            if (draft == null)
                return NotFound(ApiResponses.NotFound("Receipt draft was not found or has expired."));

            var hasAccess = await _permissionAccessService.HasCashbookOrOwnerAccessAsync(
                draft.CashBookId,
                userId.Value,
                SpendwiseSystem.Domain.Enums.CashbookPermission.TRANSACTION_CREATE.ToString());
            if (!hasAccess)
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponses.Forbidden());

            if (string.IsNullOrWhiteSpace(draft.OcrText))
                return Conflict(ApiResponses.Conflict("Analyze the receipt before requesting transaction suggestions."));

            var suggestion = _receiptParsingService.Parse(draft.OcrText);
            await _receiptDraftRepository.UpdateParsedSuggestionAsync(draftId, suggestion);

            return Ok(ApiResponses.SuccessWithData(
                new ReceiptDraftSuggestionDto
                {
                    DraftId = draft.Id,
                    Status = "ReadyForReview",
                    Merchant = suggestion.Merchant,
                    Amount = suggestion.Amount,
                    TransactionDate = suggestion.TransactionDate,
                    TransactionType = suggestion.TransactionType,
                    SuggestedCategory = suggestion.Category,
                    Confidence = suggestion.Confidence,
                    RecognizedText = draft.OcrText
                },
                "Receipt fields were extracted as suggestions. Review and correct them before saving."));
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
            catch
            {
                if (!string.IsNullOrWhiteSpace(transactionDto.FileName))
                {
                    FileUploadHelper.DeleteUploadedFile(transactionDto.FileName);
                }

                throw;
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

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponseRaw>> DeleteCashTransaction(Guid id)
        {
            var modifiedBy = _currentUserService.UserName;
            var userId = _currentUserService.UserId;
            if(userId == Guid.Empty)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var responseFromService = await _transactionService.DeleteTransaction(id, modifiedBy, userId ?? Guid.Empty);

            if (responseFromService.Success)
            {
                return Ok(ApiResponses.Success(responseFromService.Message));
            }

            return BadRequest(ApiResponses.BadRequest());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CashTransactionDto>> GetCashTransactionById(Guid id)
        {
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponses.Unauthorized());
            }

            var responseFromService = await _transactionService.GetTransactionById(id,userId ?? Guid.Empty);

            if (responseFromService.Success)
            {
                return Ok(ApiResponses.SuccessWithData(responseFromService.Data, responseFromService.Message));
            }

            return BadRequest(ApiResponses.BadRequest());
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
            catch
            {
                if (!string.IsNullOrWhiteSpace(transactionUpdateDto.FileName))
                {
                    FileUploadHelper.DeleteUploadedFile(transactionUpdateDto.FileName);
                }
                throw;
            }
        }


        [HttpGet("getfile")]
        public async Task<ActionResult<ApiResponse<CashTransactionFileDto>>> GetCashTransactionFile(string id)
        {
            var userId = _currentUserService.UserId;
            var responseFromService = await _transactionService.GetCashTransactionFile(id, userId ?? Guid.Empty);

            if (!responseFromService.Success)
                return BadRequest(ApiResponses.BadRequest());

            var filePath = FileUploadHelper.GetFileUrl(new List<string?> { responseFromService.Data.FileName }, Request).FirstOrDefault();
            responseFromService.Data.FilePath = filePath;

            return Ok(ApiResponses.SuccessWithData(responseFromService.Data, responseFromService.Message));

        }
    }
}
