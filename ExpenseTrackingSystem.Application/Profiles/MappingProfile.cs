using AutoMapper;
using SpendwiseSystem.Domain.DBOs;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.DTOs.AuthDtos;
using SpendwiseSystem.Domain.DTOs.CashTransactionDtos;
using SpendwiseSystem.Domain.Entities;

namespace SpendwiseSystem.Application.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserDto, User>();
            CreateMap<LoginResponseDbo, LoginResponseDto>();
            CreateMap<LoginResponseDto,LoginResponseDbo>();
            CreateMap<LoginResponseDbo, UserDataForTokenGeneration>();
            CreateMap<UserDataForTokenGeneration, RefreshTokenWithUserDataDto>();
            CreateMap<CashTransactionDto, CashTransaction>();
            CreateMap<CashTransaction, CashTransactionDto>();
        }
    }
}


