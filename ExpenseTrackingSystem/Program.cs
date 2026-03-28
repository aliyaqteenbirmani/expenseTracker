using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Profiles;
using SpendwiseSystem.Application.Services.AuthService;
using SpendwiseSystem.Application.Services.SpendwiseService;
using SpendwiseSystem.Application.Services.TokenService;
using SpendwiseSystem.Infrastructure.Data.DbContext;
using SpendwiseSystem.Infrastructure.Data.Migrations;
using SpendwiseSystem.Infrastructure.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Jwt Authentication middleware
var jwtSetting = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSetting["Key"] ?? throw new Exception("JWT Key missing");
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSetting["Issuer"],
            ValidAudience = jwtSetting["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Add services to the container.
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<IDapperContext, DapperContext>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionStr"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }));

builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IAuthRepository, AuthRepository>();
builder.Services.AddTransient<ISpendwiseService, SpendwiseService>();
builder.Services.AddTransient<ISpendwiseRepository, SpendwiseRepository>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<AutoMapper.IConfigurationProvider>(_ =>
{
    var configExpression = new AutoMapper.MapperConfigurationExpression();
    configExpression.AddProfile(new MappingProfile());
    return new AutoMapper.MapperConfiguration(configExpression, NullLoggerFactory.Instance);
});
builder.Services.AddSingleton<IMapper>(sp =>
    new Mapper(sp.GetRequiredService<AutoMapper.IConfigurationProvider>(), sp.GetService));
builder.Services.AddControllers();
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    logging.RequestBodyLogLimit = 4096; // Adjust as needed
    logging.ResponseBodyLogLimit = 4096; // Adjust as needed
});

builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseHttpLogging();
app.UseSwagger();
// Always enable Swagger UI
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SpendwiseSystem API v1");
    c.RoutePrefix = "swagger"; // Visit /swagger
});

app.UseCors("AllowFlutterApp");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
