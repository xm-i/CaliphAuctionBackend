using System.Net;
using System.Text.Json;
using CaliphAuctionBackend.Exceptions;

namespace CaliphAuctionBackend.Middleware;

public class CaliphExceptionHandlingMiddleware(RequestDelegate next, ILogger<CaliphExceptionHandlingMiddleware> logger) {
	private readonly ILogger<CaliphExceptionHandlingMiddleware> _logger = logger;
	private readonly RequestDelegate _next = next;

	public async Task Invoke(HttpContext context) {
		try {
			await this._next(context);
		} catch (CaliphException ex) {
			// Known (domain) exception: use its status code
			if (!context.Response.HasStarted) {
				context.Response.Clear();
				context.Response.StatusCode = (int)ex.StatusCode;
				context.Response.ContentType = "application/json";
				var payload = new { message = ex.Message, status = (int)ex.StatusCode };
				await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
			} else {
				this._logger.LogWarning(ex, "Response already started while handling CaliphException.");
			}
		} catch (Exception ex) {
			// Unexpected: 500
			this._logger.LogError(ex, "Unhandled exception");
			if (!context.Response.HasStarted) {
				context.Response.Clear();
				context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				context.Response.ContentType = "application/json";
				var payload = new { message = "Internal Server Error", status = 500 };
				await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
			}
		}
	}
}