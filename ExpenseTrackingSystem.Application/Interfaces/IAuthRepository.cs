using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<ApiResponse<User>> RegisterUser(User user);
        Task<ApiResponse<LoginResponseDbo>> Login(LoginDto loginDto);
    }
}


