using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Services.BusinessService
{
    public interface IBusinessService
    {
        Task<ApiResponse<BusinessResponseDto>> CreateBusiness(BusinessDto business, string CreatedBy, string UserId);
        Task<ApiResponse<BusinessResponseDto>> UpdateBusiness(BusinessDto business, string CreatedBy);
        Task<ApiResponse<BusinessResponseDto>> DeleteBusiness(string id, string userName);
        Task<ApiResponse<BusinessResponseDto>> GetBusiness(string id);
        Task<ApiResponse<List<BusinessResponseDto>>> GetAllBusiness(string UserId);
    }
}
