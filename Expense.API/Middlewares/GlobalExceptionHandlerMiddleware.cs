using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentValidation;
using Expense.Core.DTOs.Shared;
using Expense.Core.Common.Exceptions;

namespace Expense.API.Middlewares
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next, 
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                // Client cancelled the request or task was cancelled - don't log as error
                // This also catches TaskCanceledException since it inherits from OperationCanceledException
                _logger.LogInformation("Request or task was cancelled");
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. Request Path: {Path}, Method: {Method}, TraceId: {TraceId}", 
                    context.Request.Path, context.Request.Method, context.TraceIdentifier);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Don't write response if it has already started
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response has already started, cannot write error response");
                return;
            }

            context.Response.ContentType = "application/json";
            
            var isDevelopment = _environment.IsDevelopment();

            // Handle AggregateException by unwrapping the first inner exception
            if (exception is AggregateException aggEx && aggEx.InnerException != null)
            {
                _logger.LogError(aggEx, "AggregateException with {Count} inner exceptions, TraceId: {TraceId}", aggEx.InnerExceptions.Count, context.TraceIdentifier);
                await HandleExceptionAsync(context, aggEx.InnerException);
                return;
            }
            
            APIResponse<object> response;
            int statusCode;

            switch (exception)
            {
                case ValidationException validationEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    var validationErrors = validationEx.Errors
                        .GroupBy(e => e.PropertyName.StartsWith("Request.") ? e.PropertyName.Substring(8) : e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "Validation failed",
                        Errors = validationErrors,
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "ValidationException: {Message}, TraceId: {TraceId}", validationEx.Message, context.TraceIdentifier);
                    break;

                case BusinessException businessEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = businessEx.Message, // Always show message for business exceptions
                        Errors = new List<string> { businessEx.Message },
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "BusinessException: {Message}, TraceId: {TraceId}", businessEx.Message, context.TraceIdentifier);
                    break;

                case ArgumentNullException argNullEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "Required parameter is missing",
                        Errors = isDevelopment ? new List<string> { argNullEx.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "ArgumentNullException: {Message}, TraceId: {TraceId}", argNullEx.Message, context.TraceIdentifier);
                    break;

                case ArgumentException argEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = isDevelopment ? argEx.Message : "Invalid parameter provided",
                        Errors = isDevelopment ? new List<string> { argEx.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "ArgumentException: {Message}, TraceId: {TraceId}", argEx.Message, context.TraceIdentifier);
                    break;

                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "Resource not found",
                        Errors = isDevelopment ? new List<string> { exception.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "KeyNotFoundException: {Message}, TraceId: {TraceId}", exception.Message, context.TraceIdentifier);
                    break;

                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "Unauthorized access",
                        Errors = isDevelopment ? new List<string> { exception.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "UnauthorizedAccessException: {Message}, TraceId: {TraceId}", exception.Message, context.TraceIdentifier);
                    break;

                case InvalidOperationException invalidOpEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = isDevelopment ? invalidOpEx.Message : "Invalid operation",
                        Errors = isDevelopment ? new List<string> { invalidOpEx.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "InvalidOperationException: {Message}, TraceId: {TraceId}", invalidOpEx.Message, context.TraceIdentifier);
                    break;

                case TimeoutException:
                    statusCode = (int)HttpStatusCode.RequestTimeout;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "Request timeout",
                        Errors = isDevelopment ? new List<string> { exception.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "TimeoutException: {Message}, TraceId: {TraceId}", exception.Message, context.TraceIdentifier);
                    break;

                case FormatException formatEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "Invalid format",
                        Errors = isDevelopment ? new List<string> { formatEx.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "FormatException: {Message}, TraceId: {TraceId}", formatEx.Message, context.TraceIdentifier);
                    break;

                case NotImplementedException:
                    statusCode = (int)HttpStatusCode.NotImplemented;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "Feature not implemented",
                        Errors = isDevelopment ? new List<string> { exception.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "NotImplementedException: {Message}, TraceId: {TraceId}", exception.Message, context.TraceIdentifier);
                    break;

                case BadHttpRequestException badHttpEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "Invalid HTTP request",
                        Errors = isDevelopment ? new List<string> { badHttpEx.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "BadHttpRequestException: {Message}, TraceId: {TraceId}", badHttpEx.Message, context.TraceIdentifier);
                    break;

                case HttpRequestException httpEx:
                    statusCode = (int)HttpStatusCode.BadGateway;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "External service request failed",
                        Errors = isDevelopment ? new List<string> { httpEx.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogError(exception, "HttpRequestException: {Message}, TraceId: {TraceId}", httpEx.Message, context.TraceIdentifier);
                    break;

                case DbUpdateConcurrencyException concurrencyEx:
                    statusCode = (int)HttpStatusCode.Conflict;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "The resource was modified by another user. Please refresh and try again.",
                        Errors = isDevelopment ? new List<string> { concurrencyEx.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogWarning(exception, "DbUpdateConcurrencyException: {Message}, TraceId: {TraceId}", concurrencyEx.Message, context.TraceIdentifier);
                    break;

                case DbUpdateException dbEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "Database operation failed",
                        Errors = isDevelopment ? new List<string> { dbEx.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogError(exception, "DbUpdateException: {Message}, TraceId: {TraceId}", dbEx.Message, context.TraceIdentifier);
                    break;

                case SqlException sqlEx:
                    string sqlMessage;
                    // Handle specific SQL Server error numbers
                    switch (sqlEx.Number)
                    {
                        case 2601: // Unique constraint violation (duplicate key)
                        case 2627: // Unique constraint violation (duplicate key)
                            statusCode = (int)HttpStatusCode.BadRequest;
                            sqlMessage = "A record with this value already exists";
                            break;
                        case 547: // Foreign key constraint violation
                            statusCode = (int)HttpStatusCode.BadRequest;
                            sqlMessage = "Cannot delete or update due to related records";
                            break;
                        case 2: // Timeout expired
                            statusCode = (int)HttpStatusCode.RequestTimeout;
                            sqlMessage = "Database operation timed out";
                            break;
                        case 17: // Server not found or not accessible
                        case 53: // Network-related error
                            statusCode = (int)HttpStatusCode.ServiceUnavailable;
                            sqlMessage = "Database service is temporarily unavailable";
                            break;
                        default:
                            statusCode = (int)HttpStatusCode.BadRequest;
                            sqlMessage = "Database error occurred";
                            break;
                    }

                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = sqlMessage,
                        Errors = isDevelopment ? new List<string> { sqlEx.Message } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogError(exception, "SqlException: {Message}, Error Number: {Number}, TraceId: {TraceId}", sqlEx.Message, sqlEx.Number, context.TraceIdentifier);
                    break;

                case OutOfMemoryException:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "Server is experiencing high load. Please try again later.",
                        Errors = new List<string>(), // Never expose OOM details
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogCritical(exception, "OutOfMemoryException: Critical memory issue, TraceId: {TraceId}", context.TraceIdentifier);
                    break;

                case StackOverflowException:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "An internal error occurred",
                        Errors = new List<string>(), // Never expose stack overflow details
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogCritical(exception, "StackOverflowException: Critical stack overflow, TraceId: {TraceId}", context.TraceIdentifier);
                    break;

                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    response = new APIResponse<object>
                    {
                        Success = false,
                        Data = null,
                        StatusCode = statusCode,
                        Message = "An internal server error occurred",
                        Errors = isDevelopment ? new List<string> { exception.Message, exception.StackTrace ?? string.Empty } : new List<string>(),
                        TraceId = context.TraceIdentifier
                    };
                    context.Response.StatusCode = statusCode;
                    _logger.LogError(exception, "Unhandled exception: {Message}\nStackTrace: {StackTrace}, TraceId: {TraceId}", 
                        exception.Message, exception.StackTrace, context.TraceIdentifier);
                    break;
            }

            try
            {
                var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(jsonResponse);
            }
            catch (Exception writeEx)
            {
                _logger.LogError(writeEx, "Failed to write error response to client, TraceId: {TraceId}", context.TraceIdentifier);
            }
        }
    }
}