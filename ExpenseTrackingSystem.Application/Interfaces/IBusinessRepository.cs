using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface IBusinessRepository
    {
        Task<SPResponseFromDb> CreateBusiness(BusinessDto business, string CreatedBy, string UserId);
        Task<SPResponseFromDb> UpdateBusiness(BusinessDto business, string CreatedBy);
        Task<SPResponseFromDb> DeleteBusiness(string id, string userName);
        Task<SPResponseFromDb> GetBusiness(string id);    
        Task<SPResponseFromDb> GetAllBusiness(string UserId);    
    }
}
