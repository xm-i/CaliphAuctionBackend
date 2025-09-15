using System.Security.Claims;
using CaliphAuctionBackend.Dtos.AuctionItem;
using CaliphAuctionBackend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaliphAuctionBackend.Controllers;

[ApiController]
[Route("auction")]
public class AuctionController(IAuctionService auctionService) : ControllerBase {
	private readonly IAuctionService _auctionService = auctionService;

	[HttpGet("search")]
	[AllowAnonymous]
	public async Task<ActionResult<SearchAuctionItemsResponse>> SearchAsync([FromQuery] int limit, int? categoryId) {
		var result = await this._auctionService.SearchAsync(limit, categoryId);
		return this.Ok(result);
	}

	[HttpGet("items/{id:int}")]
	[AllowAnonymous]
	public async Task<ActionResult<AuctionItemDetailDto>> GetDetailAsync([FromRoute] int id) {
		var dto = await this._auctionService.GetDetailAsync(id);
		return this.Ok(dto);
	}

	[HttpGet("categories")]
	[AllowAnonymous]
	public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategoriesAsync() {
		var list = await this._auctionService.GetCategoriesAsync();
		return this.Ok(list);
	}

	[HttpPost("place-bid")]
	[Authorize]
	public async Task<IActionResult> PlaceBidAsync([FromBody] PlaceBidRequest request) {
		var ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
		var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (!int.TryParse(userIdClaim, out var userId)) {
			return this.Unauthorized();
		}

		await this._auctionService.PlaceBidAsync(userId, request, ipAddress);
		return this.NoContent();
	}
}