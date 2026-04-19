using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs.BusinessDtos;
using SpendwiseSystem.Domain.Entities;
using SpendwiseSystem.Infrastructure.Data.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Infrastructure.Repositories
{
    public class BusinessRepository : IBusinessRepository
    {
        private readonly IDapperContext _context;

        public BusinessRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<SPResponseFromDb> CreateBusiness(BusinessDto business, string CreatedBy,string UserId)
        {
            return await _context.GetSingleAsync<SPResponseFromDb>("SP_CreateBusiness",
               new
               {
                   Name = business.Name,
                   Category = business.Category,
                   Description = business.Description,
                   FileName = business.FileName,
                   CreatedBy = CreatedBy,
                   UserId = Guid.Parse(UserId)
               },
               commandType: System.Data.CommandType.StoredProcedure);
        }

        public Task<SPResponseFromDb> GetAllBusiness(string UserId)
        {
            return _context.GetSingleAsync<SPResponseFromDb>("SP_GetAllBusinessesByUserId", new { UserId = Guid.Parse(UserId) }, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<SPResponseFromDb> GetBusiness(string id)
        {
            return await _context.GetSingleAsync<SPResponseFromDb>("SP_GetBusinessById", 
                new 
                { Id = Guid.Parse(id) }, 
                commandType: System.Data.CommandType.StoredProcedure);
        }


        public async Task<SPResponseFromDb> DeleteBusiness(string id,string userName)
        {
            return await _context.GetSingleAsync<SPResponseFromDb>("SP_DeleteBusinessById", new { Id = Guid.Parse(id),ModifiedBy = userName }, commandType: System.Data.CommandType.StoredProcedure);
        }
        
        public async Task<SPResponseFromDb> UpdateBusiness(BusinessDto business, string CreatedBy)
        {
            return await _context.GetSingleAsync<SPResponseFromDb>("SP_UpdateBusiness",
                   new
                   {
                       Id = business.Id,
                       Name = business.Name,
                       Category = business.Category,
                       Description = business.Description,
                       FileName = business.FileName,
                       ModifiedBy = CreatedBy,
                       
                   },
                    commandType: System.Data.CommandType.StoredProcedure);
        }
    }
}
