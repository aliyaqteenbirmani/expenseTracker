using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Services.BusinessService;
using SpendwiseSystem.Application.Services.CurrentUserService;
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
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _environment;

        public BusinessController(IBusinessService businessService, IWebHostEnvironment environment, ICurrentUserService currentUser)
        {
            _businessService = businessService;
            _environment = environment;
            _currentUser = currentUser;
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<BusinessResponseDto>> CreateBusiness([FromForm] BusinessDto business)
        {
            var CreatedBy = _currentUser.UserName;
            var userId = _currentUser.UserId.ToString();

            var result = await _businessService.CreateBusiness(business, CreatedBy, userId);

            if (!result.Success)
                return Conflict(ApiResponses.Conflict(result.Message));

            var fileName = result.Data.FileName;
            var fileUrl = FileUploadHelper.GetFileUrl(new List<string> { fileName }, Request);
            result.Data.FileUrl = fileUrl.FirstOrDefault();

            // Fix: Use Created() instead of CreatedAtAction() since you don't have a route or action name to reference.
            return Created(string.Empty, ApiResponses.Created(result.Data, result.Message));
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetBusiness(string id)
        {
            var userId = _currentUser.UserId;
            if(userId == Guid.Empty)
                return Unauthorized(ApiResponses.Unauthorized());

            var result = await _businessService.GetBusiness(id);
            if (!result.Success)
                return BadRequest( ApiResponses.BadRequest(result.Message));

            return Ok(ApiResponses.SuccessWithData(result.Data, result.Message));
        }

        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<List<BusinessResponseDto>>>> GetAllBusinesses()
        {
            var UserId = _currentUser.UserId.ToString();

            if (UserId == Guid.Empty.ToString())
                return Unauthorized( ApiResponses.Unauthorized());

            try
            {
                var result = await _businessService.GetAllBusiness(UserId);
                if (result.StatusCode == 400)
                    return NotFound(ApiResponses.NotFound("There are no businesses of this User"));

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
                    return BadRequest( ApiResponses.BadRequest(result.Message));


                return Ok( ApiResponses.SuccessWithData(result.Data, result.Message));
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
            var userName = _currentUser.UserName;
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
                return BadRequest( ApiResponses.BadRequest(result.Message));

            result.Data.FileUrl =  FileUploadHelper.GetFileUrl(new List<string> { result.Data.FileName }, Request).FirstOrDefault();

            return Ok(
                ApiResponses.SuccessWithData(result.Data, result.Message)); 

        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<BusinessResponseDto>>> DeleteBusiness(string id)
        {
            var userName = _currentUser.UserName;
            var result = await _businessService.DeleteBusiness(id, userName);

            if (!result.Success)
                return BadRequest( ApiResponses.BadRequest(result.Message)); 
            
            return Ok(ApiResponses.SuccessWithData(result.Data, result.Message));
        }

    }
}