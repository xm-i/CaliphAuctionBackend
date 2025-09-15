using System.Diagnostics;
using System.Net;
using System.Security.Claims;

namespace CaliphAuctionBackend.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger) {
	private readonly ILogger<RequestLoggingMiddleware> _logger = logger;
	private readonly RequestDelegate _next = next;

	public async Task Invoke(HttpContext context) {
		var sw = Stopwatch.StartNew();
		var req = context.Request;
		var route = string.Empty;
		var userId = context.User?.Identity?.IsAuthenticated == true ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value : "anon";
		this._logger.LogInformation("REQ {Method} {Path}{Query} user={User}", req.Method, req.Path, req.QueryString.HasValue ? req.QueryString.Value : string.Empty, userId);
		var status = 500;
		try {
			await this._next(context);
			status = context.Response.StatusCode;
		} catch (Exception ex) {
			status = context.Response.HasStarted ? context.Response.StatusCode : (int)HttpStatusCode.InternalServerError;
			this._logger.LogError(ex, "Unhandled exception for {Method} {Path} user={User}", req.Method, req.Path, userId);
			throw; // let exception middleware format response
		} finally {
			sw.Stop();
			var elapsedMs = sw.ElapsedMilliseconds;
			// Endpoint routing info (after next) if available
			route = context.GetEndpoint()?.DisplayName ?? route;
			this._logger.LogInformation("RES {Method} {Path} status={Status} {Elapsed}ms route={Route} user={User}", req.Method, req.Path, status, elapsedMs, route, userId);
		}
	}
}