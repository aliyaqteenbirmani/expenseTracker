using AutoMapper;
using CashBookSystem.Application.Interfaces;
using CashBookSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using SpendwiseSystem.API.Extensions;
using SpendwiseSystem.Application.Interfaces;
using SpendwiseSystem.Application.Profiles;
using SpendwiseSystem.Application.Services.AuthService;
using SpendwiseSystem.Application.Services.BusinessService;
using SpendwiseSystem.Application.Services.SpendwiseService;
using SpendwiseSystem.Application.Services.TokenService;
using SpendwiseSystem.Application.Services.TransactionService;
using SpendwiseSystem.Domain.DTOs;
using SpendwiseSystem.Domain.Entities.ConfigModel;
using SpendwiseSystem.Infrastructure.Data.DbContext;
using SpendwiseSystem.Infrastructure.Data.Migrations;
using SpendwiseSystem.Infrastructure.Repositories;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<AppLinkSettings>(builder.Configuration.GetSection("PasswordResetBaseUrl"));
builder.Services.AddControllers();
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    logging.RequestBodyLogLimit = 4096; // Adjust as needed
    logging.ResponseBodyLogLimit = 4096; // Adjust as needed
});

//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Spendwise API",
        Version = "v1"
    });

    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by space and your JWT token."
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    };

    c.AddSecurityRequirement(securityRequirement);
}); 
var app = builder.Build();

app.UseHttpLogging();
app.UseSwagger();
// Always enable Swagger UI
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SpendwiseSystem API v1");
    c.RoutePrefix = "swagger"; 
});

app.UseCors("AllowFlutterApp");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
