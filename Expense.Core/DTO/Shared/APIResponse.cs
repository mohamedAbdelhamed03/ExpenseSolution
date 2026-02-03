using System;
using System.Collections.Generic;

namespace Expense.Core.DTO.Shared
{
	public class APIResponse<T>
	{
		public bool Success { get; init; }
		public string? Message { get; init; }
		public T? Data { get; init; }
		public object Errors { get; init; } = new List<string>();
		public int StatusCode { get; init; }
		public DateTime Timestamp { get; init; } = DateTime.UtcNow;
		public string? TraceId { get; init; }
		public Dictionary<string, object>? Meta { get; init; }

		// Success response with typed data
		public static APIResponse<T> SuccessResponse(T? data = default, string? message = null, int statusCode = 200, string? traceId = null, Dictionary<string, object>? meta = null)
		{
			return new APIResponse<T>
			{
				Success = true,
				Data = data,
				Message = message ?? "Operation completed successfully",
				StatusCode = statusCode,
				TraceId = traceId,
				Meta = meta
			};
		}

		// Error response with message
		public static APIResponse<T> ErrorResponse(string message, T? data = default, int statusCode = 400, string? traceId = null, Dictionary<string, object>? meta = null)
		{
			return new APIResponse<T>
			{
				Success = false,
				Data = data,
				Errors = new List<string> { message },
				StatusCode = statusCode,
				TraceId = traceId,
				Meta = meta
			};
		}

		public static APIResponse<T> Failure(IEnumerable<string> errors, string? message = null, int statusCode = 400, string? traceId = null, Dictionary<string, object>? meta = null)
		{
			return new APIResponse<T>
            {
                Success = false,
                Message = message,
                Errors = new List<string>(errors),
                StatusCode = statusCode,
                TraceId = traceId,
                Meta = meta
            };
		}

		public static APIResponse<T> FromException(Exception ex, int statusCode = 500, string? traceId = null, Dictionary<string, object>? meta = null)
		{
			return new APIResponse<T>
			{
				Success = false,
				Errors = new List<string> { ex.Message },
				StatusCode = statusCode,
				TraceId = traceId,
				Meta = meta
			};
		}

		public static implicit operator APIResponse<T>(T data)
		{
			return SuccessResponse(data);
		}
	}
}
