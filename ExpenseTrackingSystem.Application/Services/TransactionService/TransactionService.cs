using AutoMapper;
using SpendwiseSystem.Application.Helper;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Services.FileUploadHelper;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.CashTransactionDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SpendwiseSystem.Application.Services.TransactionService
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IMapper _mapper;

        public TransactionService(ITransactionRepository transactionRepository, IMapper mapper)
        {
            _transactionRepository = transactionRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<CashTransaction>> AddCashTransaction(CashTransactionDto transactionDto, string createdBy)
        {
            var resultFromRepo = await _transactionRepository.AddCashTransaction(transactionDto, createdBy);

            if (resultFromRepo.Success)
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<CashTransaction>(resultFromRepo.Data, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

                return new ApiResponse<CashTransaction>
                {
                    StatusCode = resultFromRepo.ResponseCode,
                    Success = true,
                    Message = resultFromRepo.Message,
                    ErrorCode = null,
                    Data = data
                };
            }

            return new ApiResponse<CashTransaction>
            {
                StatusCode = resultFromRepo.ResponseCode,
                Success = false,
                Message = resultFromRepo.Message,
                ErrorCode = resultFromRepo.Message,
                Data = null
            };
        }

        public async Task<ApiResponseRaw> DeleteTransaction(Guid id, string modifiedBy)
        {
            var responseFromRepo = await _transactionRepository.DeleteTransactionAsync(id, modifiedBy);
            if (!responseFromRepo.Success)
            {
                return new ApiResponseRaw
                {
                    Success = false,
                    Message = responseFromRepo.Message,
                    ResponseCode = responseFromRepo.ResponseCode
                };
               
            }
            FileUploadHelper.FileUploadHelper.DeleteUploadedFile(responseFromRepo.Data);
            return new ApiResponseRaw
            {
                Success = true,
                ResponseCode = responseFromRepo.ResponseCode,
                Message = responseFromRepo.Message
            };

        }

        public async Task<ApiResponse<CashTransactionDto>> GetTransactionById(Guid id)
        {
            var responseFromRepo = await _transactionRepository.GetTransactionAsync(id);
            if(responseFromRepo.Success)
            {
                var mappedTransaction = _mapper.Map<CashTransactionDto>(responseFromRepo.Data);
                return new ApiResponse<CashTransactionDto>
                {
                    Success = true,
                    StatusCode = ApiResponses.Success().StatusCode,
                    Message = "Cash Transaction fetched successfully.",
                    Data = mappedTransaction
                };
            }

            return new ApiResponse<CashTransactionDto>
            {
                Success = false,
                StatusCode = ApiResponses.BadRequest().StatusCode,
                Message = "Cash Transaction fetched successfully.",
                Data = null
            };
        }


        public async Task<ApiResponse<CashTransactionFileDto>> GetCashTransactionFile(string id)
        {
            var responseFromRepo = await _transactionRepository.GetTransactionFileName(id);
            if (responseFromRepo.Success)
            {
                // Deserialize the string data to CashTransactionFileDto
                var data = new CashTransactionFileDto { FileName = responseFromRepo.Data };

                return new ApiResponse<CashTransactionFileDto>
                {
                    Success = true,
                    StatusCode = ApiResponses.Success().StatusCode,
                    Message = responseFromRepo.Message,
                    Data = data
                };
            }

            return new ApiResponse<CashTransactionFileDto>
            {
                Success = false,
                StatusCode = ApiResponses.BadRequest().StatusCode,
                Message = responseFromRepo.Message,
                Data = null
            };
        }

        public async Task<ApiResponse<CTUpdateResponseDto>> UpdateCashTransaction(CashTransactionUpdateDto transactionUpdateDto, string modifiedBy)
        {
            var responseFromRepo = await  _transactionRepository.UpdateCashTransaction(transactionUpdateDto, modifiedBy);

            if (!responseFromRepo.Success)
            {
                return new ApiResponse<CTUpdateResponseDto>
                {
                    Success = false,
                    Message = responseFromRepo.Message,
                    StatusCode = ApiResponses.BadRequest().StatusCode,
                    Data = null
                };
            }
            
            var cashTransaction = JsonSerializer.Deserialize<CTUpdateResponseDto>(responseFromRepo.Data);

            if (!string.IsNullOrWhiteSpace(cashTransaction.OldFileName))
            {
                FileUploadHelper.FileUploadHelper.DeleteUploadedFile(cashTransaction.OldFileName);
            }

            return new ApiResponse<CTUpdateResponseDto>
            {
                Success = true,
                StatusCode = ApiResponses.Success().StatusCode,
                Message = responseFromRepo.Message,
                Data = cashTransaction
            };
        }

        public async Task<ApiResponse<List<AllCashTransactionDto>>> GetAllTransactionsOfCashBook(string CashBookId)
        {
            var responseFromRepo = await _transactionRepository.GetAllTransactionsOfCashBook(CashBookId);
            if (responseFromRepo.Success)
            {
                List<AllCashTransactionDto> transactions = new List<AllCashTransactionDto>();
                transactions = JsonSerializer.Deserialize<List<AllCashTransactionDto>>(responseFromRepo.Data, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                var transactionList = _mapper.Map<List<AllCashTransactionDto>>(transactions);

                return new ApiResponse<List<AllCashTransactionDto>>
                {
                    Success = true,
                    StatusCode = ApiResponses.Success().StatusCode,
                    Message = responseFromRepo.Message,
                    Data = transactionList
                };
            }

            return new ApiResponse<List<AllCashTransactionDto>>
            {
                Success = false,
                StatusCode = ApiResponses.BadRequest().StatusCode,
                Message = responseFromRepo.Message,
                Data = null
            };
        }
    }
}
