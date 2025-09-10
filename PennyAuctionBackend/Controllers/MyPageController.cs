using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PennyAuctionBackend.Dtos.AuctionItem;
using PennyAuctionBackend.Dtos.MyPage;
using PennyAuctionBackend.Services.Interfaces;

namespace PennyAuctionBackend.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public class MyPageController(IMyPageService myPageService) : ControllerBase {
	private readonly IMyPageService _myPageService = myPageService;

	[HttpGet("summary")]
	public async Task<ActionResult<MyPageSummaryDto>> GetSummary() {
		var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(userIdStr, out var userId)) {
			return this.Unauthorized();
		}

		var result = await this._myPageService.GetSummaryAsync(userId);
		return this.Ok(result);
	}

	[HttpGet("bidding-items")]
	public async Task<ActionResult<SearchAuctionItemsResponse>> GetBiddingItems([FromQuery] int? limit) {
		var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(userIdStr, out var userId)) {
			return this.Unauthorized();
		}

		var list = await this._myPageService.GetBiddingItemsAsync(userId, limit);
		return this.Ok(list);
	}

	[HttpGet("won-items")]
	public async Task<ActionResult<SearchAuctionItemsResponse>> GetWonItems([FromQuery] int? limit) {
		var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(userIdStr, out var userId)) {
			return this.Unauthorized();
		}

		var list = await this._myPageService.GetWonItemsAsync(userId, limit);
		return this.Ok(list);
	}
}