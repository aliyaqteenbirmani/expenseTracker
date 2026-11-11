using AutoMapper;
using CashBookSystem.Application.Interfaces;
using CashBookSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Profiles;
using SpendwiseSystem.Application.Services.AuthService;
using SpendwiseSystem.Application.Services.BusinessService;
using SpendwiseSystem.Application.Services.CurrentUserService;
using SpendwiseSystem.Application.Services.EmailService;
using SpendwiseSystem.Application.Services.InvitationEmailService;
using SpendwiseSystem.Application.Services.InvitationLinkBuilder;
using SpendwiseSystem.Application.Services.InvitationService;
using SpendwiseSystem.Application.Services.MemberManagementService;
using SpendwiseSystem.Application.Services.PermissionAccessService;
using SpendwiseSystem.Application.Services.SpendwiseService;
using SpendwiseSystem.Application.Services.SplitExpenses;
using SpendwiseSystem.Application.Services.TokenService;
using SpendwiseSystem.Application.Services.TransactionService;
using SpendwiseSystem.Application.Services.ReceiptOcrService;
using SpendwiseSystem.Application.Services.ReceiptParsingService;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities.ConfigModel;
using SpendwiseSystem.Infrastructure.Data.DbContext;
using SpendwiseSystem.Infrastructure.Data.Migrations;
using SpendwiseSystem.Infrastructure.Repositories;
using SpendwiseSystem.Infrastructure.Services.ReceiptOcrService;
using SpendwiseSystem.Infrastructure.Services.ReceiptParsingService;
using SpendwiseSystem.API.Services;

namespace SpendwiseSystem.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IDapperContext, DapperContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnectionStr"),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null
                        );
                    }));
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IAuthRepository, AuthRepository>();

            services.AddTransient<ICashBookService, CashBookService>();
            services.AddTransient<ICashBookRepository, CashBookRepository>();

            services.AddTransient<ITransactionService, TransactionService>();
            services.AddTransient<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IReceiptDraftRepository, ReceiptDraftRepository>();
            services.AddScoped<IReceiptOcrService, TesseractReceiptOcrService>();
            services.AddScoped<IReceiptParsingService, RuleBasedReceiptParsingService>();
            services.AddHostedService<ReceiptDraftCleanupService>();

            services.AddTransient<IBusinessService, BusinessService>();
            services.AddTransient<IBusinessRepository, BusinessRepository>();

            services.AddTransient<IInvitationService, InvitationService>();
            services.AddTransient<IInvitationRepository, InvitationRepository>();

            services.Configure<SpendwiseSystem.Domain.DTOs.SmtpSettings>(configuration.GetSection("SmtpSettings"));
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IInvitationEmailService, InvitationEmailService>();

            services.AddSingleton<ITokenService, TokenService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton(configuration.GetSection("AppLinkSettings").Get<AppLinkSettings>());
            services.AddTransient<IInvitationLinkBuilder, InvitationLinkBuilder>();

            services.AddTransient<IMemberManagementRepository, MemberManagementRepository>();
            services.AddTransient<IMemberManagementService, MemberManagementService>();

            services.AddTransient<IPermissionAccessRepository, PermissionAccessRepository>();
            services.AddTransient<IPermissionAccessService, PermissionAccessService>();

            services.AddTransient<ISplitGroupRepository, SplitGroupRepository>();
            services.AddTransient<ISplitGroupService, SplitGroupService>();
            services.AddTransient<ISplitGroupAuthorizationService, SplitGroupAuthorizationService>();
            services.AddTransient<ISplitCalculatorService, EqualSplitCalculatorService>();
            services.AddTransient<ISplitExpenseRepository, SplitExpenseRepository>();
            services.AddTransient<ISplitExpenseService, SplitExpenseService>();
            services.AddTransient<ISplitBalanceRepository, SplitBalanceRepository>();
            services.AddTransient<ISplitBalanceService, SplitBalanceService>();
            services.AddTransient<IDebtSimplificationService, DebtSimplificationService>();
            services.AddTransient<ISplitSettlementRepository, SplitSettlementRepository>();
            services.AddTransient<ISplitSettlementService, SplitSettlementService>();

            services.AddSingleton<AutoMapper.IConfigurationProvider>(_ =>
            {
                var configExpression = new MapperConfigurationExpression();
                configExpression.AddProfile(new MappingProfile());
                return new MapperConfiguration(configExpression, NullLoggerFactory.Instance);
            });

            services.AddSingleton<IMapper>(sp =>
                new Mapper(
                    sp.GetRequiredService<AutoMapper.IConfigurationProvider>(),
                    sp.GetService));

            return services;
        }
    }
}
