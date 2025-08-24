using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PennyAuctionBackend.Dtos.AuctionItem;
using PennyAuctionBackend.Service.Interfaces;

namespace PennyAuctionBackend.Controllers;

[ApiController]
[Route("auction")]
public class AuctionController(IAuctionService auctionItemService) : ControllerBase {
	private readonly IAuctionService _auctionItemService = auctionItemService;

	[HttpGet("search")]
	[AllowAnonymous]
	public async Task<ActionResult<SearchAuctionItemsResponse>> SearchAsync([FromQuery] int categoryId) {
		var result = await this._auctionItemService.SearchAsync(categoryId);
		return this.Ok(result);
	}
}