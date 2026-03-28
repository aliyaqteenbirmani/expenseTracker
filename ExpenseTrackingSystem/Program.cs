using AutoMapper;
using ExpenseTrackingSystem.Application.Interfaces;
using ExpenseTrackingSystem.Application.Profiles;
using ExpenseTrackingSystem.Application.Services.AuthService;
using ExpenseTrackingSystem.Application.Services.CashBookService;
using ExpenseTrackingSystem.Application.Services.TokenService;
using ExpenseTrackingSystem.Infrastructure.Data.DbContext;
using ExpenseTrackingSystem.Infrastructure.Data.Migrations;
using ExpenseTrackingSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddScoped<IDapperContext, DapperContext>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnectionStr"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    ));

builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IAuthRepository, AuthRepository>();
builder.Services.AddTransient<ICashBookService, CashBookService>();
builder.Services.AddTransient<ICashBookRepository, CashBookRepository>();
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
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpLogging();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Tracking System API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFlutterApp");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
