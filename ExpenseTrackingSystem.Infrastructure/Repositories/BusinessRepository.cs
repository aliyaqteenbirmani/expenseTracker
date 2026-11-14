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

        public async Task<SPResponseFromDb> CreateBusiness(BusinessDto business, string CreatedBy, string UserId)
        {
            var uid = Guid.TryParse(UserId, out var parsedUid) ? parsedUid : Guid.Empty;
            return await _context.GetSingleAsync<SPResponseFromDb>("SP_CreateBusiness",
               new
               {
                   Name = business.Name,
                   Category = business.Category,
                   Description = business.Description,
                   FileName = business.FileName,
                   CreatedBy = CreatedBy,
                   UserId = uid
               },
               commandType: System.Data.CommandType.StoredProcedure);
        }

        public Task<SPResponseFromDb> GetAllBusiness(string UserId)
        {
            var uid = Guid.TryParse(UserId, out var parsedUid) ? parsedUid : Guid.Empty;
            return _context.GetSingleAsync<SPResponseFromDb>("SP_GetAllBusinessesByUserId", new { UserId = uid }, commandType: System.Data.CommandType.StoredProcedure);
        }

        public async Task<SPResponseFromDb> GetBusiness(string id)
        {
            var bid = Guid.TryParse(id, out var parsedId) ? parsedId : Guid.Empty;
            return await _context.GetSingleAsync<SPResponseFromDb>("SP_GetBusinessById", 
                new 
                { Id = bid }, 
                commandType: System.Data.CommandType.StoredProcedure);
        }


        public async Task<SPResponseFromDb> DeleteBusiness(string id, string userName)
        {
            var bid = Guid.TryParse(id, out var parsedId) ? parsedId : Guid.Empty;
            return await _context.GetSingleAsync<SPResponseFromDb>("SP_DeleteBusinessById", new { Id = bid, ModifiedBy = userName }, commandType: System.Data.CommandType.StoredProcedure);
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
