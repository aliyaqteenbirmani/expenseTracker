using AutoMapper;
using ExpenseTrackingSystem.Domain.DBOs;
using ExpenseTrackingSystem.Domain.DTOs;
using ExpenseTrackingSystem.Domain.Entities;

namespace ExpenseTrackingSystem.Application.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserDto, User>();
            CreateMap<LoginResponseDbo, LoginResponseDto>();
            CreateMap<LoginResponseDbo, UserDataForTokenGeneration>();
        }
    }
}
