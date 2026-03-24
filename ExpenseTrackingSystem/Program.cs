using ExpenseTrackingSystem.Application.Interfaces;
using ExpenseTrackingSystem.Application.Services.AuthService;
using ExpenseTrackingSystem.Application.Services.TokenService;
using ExpenseTrackingSystem.Infrastructure.Data.DbContext;
using ExpenseTrackingSystem.Infrastructure.Data.Migrations;
using ExpenseTrackingSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
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

// Add services to the container.
builder.Services.AddScoped<IDapperContext,DapperContext>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnectionStr"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnectionStr"))
    ));
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IAuthRepository, AuthRepository>();
builder.Services.AddSingleton<ITokenService, TokenService>();
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Tracking System API v1");
    c.RoutePrefix = "swagger"; // Visit /swagger
});
app.UseCors("AllowFlutterApp");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
