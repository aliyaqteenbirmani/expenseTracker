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
        Task<ApiResponse<BusinessDto>> CreateBusiness(BusinessDto business, string CreatedBy);
        Task<ApiResponse<BusinessDto>> UpdateBusiness(BusinessDto business, string CreatedBy);
        Task<ApiResponse<BusinessDto>> DeleteBusiness(string id, string userName);
        Task<ApiResponse<BusinessDto>> GetBusiness(string id);
        Task<ApiResponse<List<BusinessDto>>> GetAllBusiness(string UserId);
    }
}
