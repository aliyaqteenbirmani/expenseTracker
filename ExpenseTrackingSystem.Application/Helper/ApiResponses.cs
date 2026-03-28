using SpendwiseSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpendwiseSystem.Application.Helper
{
    public static class ApiResponses
    {
        // Success Responses
        public static ApiResponse<object> Success(string message = "Success") => new()
        {
            StatusCode = 200,
            Success = true,
            Message = message,
            Data = new { }
        };

        public static ApiResponse<T> SuccessWithData<T>(T data, string message = "Success") => new()
        {
            StatusCode = 200,
            Success = true,
            Message = message,
            Data = data
        };

        public static ApiResponse<T> Created<T>(T data, string message = "Resource created successfully") => new()
        {
            StatusCode = 201,
            Success = true,
            Message = message,
            Data = data
        };

        // Client Error Responses
        public static ApiResponse<object> BadRequest(string message = "Invalid request parameters", string? errorCode = "INVALID_INPUT") => new()
        {
            StatusCode = 400,
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Data = new { }
        };

        public static ApiResponse<object> Unauthorized(string message = "Authentication required") => new()
        {
            StatusCode = 401,
            Success = false,
            Message = message,
            ErrorCode = "UNAUTHORIZED",
            Data = new { }
        };

        public static ApiResponse<object> Forbidden(string message = "Access denied") => new()
        {
            StatusCode = 403,
            Success = false,
            Message = message,
            ErrorCode = "FORBIDDEN",
            Data = new { }
        };

        public static ApiResponse<object> NotFound(string message = "Resource not found") => new()
        {
            StatusCode = 404,
            Success = false,
            Message = message,
            ErrorCode = "NOT_FOUND",
            Data = new { }
        };

        public static ApiResponse<object> Conflict(string message = "Resource already exists") => new()
        {
            StatusCode = 409,
            Success = false,
            Message = message,
            ErrorCode = "CONFLICT",
            Data = new { }
        };

        public static ApiResponse<object> ValidationError(object errors, string message = "Validation failed") => new()
        {
            StatusCode = 422,
            Success = false,
            Message = message,
            ErrorCode = "VALIDATION_ERROR",
            Data = errors
        };

        // Server Error Responses
        public static ApiResponse<object> InternalServerError(string message = "Something went wrong on the server") => new()
        {
            StatusCode = 500,
            Success = false,
            Message = message,
            ErrorCode = "INTERNAL_SERVER_ERROR",
            Data = new { }
        };

        public static ApiResponse<object> ServiceUnavailable(string message = "Service temporarily unavailable") => new()
        {
            StatusCode = 503,
            Success = false,
            Message = message,
            ErrorCode = "SERVICE_UNAVAILABLE",
            Data = new { }
        };
    }
}


