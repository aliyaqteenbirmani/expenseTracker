using AutoMapper;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Profiles
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


