using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CaliphAuctionBackend.Dtos.Payments;
using CaliphAuctionBackend.Dtos.Points;
using CaliphAuctionBackend.Services.Interfaces;

namespace CaliphAuctionBackend.Controllers;

[ApiController]
[Route("points")]
public class PointsController(IPointService pointService) : ControllerBase {
	private readonly IPointService _pointService = pointService;

	[HttpGet("plans")]
	[AllowAnonymous]
	public async Task<ActionResult<IReadOnlyCollection<PointPlanDto>>> GetPlansAsync() {
		var plans = await this._pointService.GetPlansAsync();
		return this.Ok(plans);
	}

	[HttpPost("purchase")]
	[Authorize]
	public async Task<ActionResult<RedeemDepositResponse>> PurchaseAsync([FromBody] RedeemDepositRequest request) {
		var principal = this.HttpContext.User;
		var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(userIdStr, out var userId)) {
			return this.Unauthorized();
		}

		var result = await this._pointService.PurchaseAsync(userId, request);
		return this.Ok(result);
	}
}