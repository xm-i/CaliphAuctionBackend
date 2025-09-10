using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PennyAuctionBackend.Dtos.Purchases;
using PennyAuctionBackend.Exceptions;
using PennyAuctionBackend.Services.Interfaces;

namespace PennyAuctionBackend.Controllers;

[ApiController]
[Route("purchases")]
[Authorize]
public class PurchasesController(IPurchaseService purchaseService) : ControllerBase {
	private readonly IPurchaseService _purchaseService = purchaseService;

	[HttpPost("won")]
	public async Task<ActionResult> PurchaseWonAsync([FromBody] PurchaseWonProductRequest request) {
		if (!this.ModelState.IsValid) {
			return this.BadRequest(this.ModelState);
		}

		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		if (request.DeliveryDate < today || request.DeliveryDate > today.AddDays(15)) {
			throw new ValidationPennyException("DeliveryDate must be within 15 days from today.");
		}

		if (request.DeliveryTimeSlot is < 1 or > 7) {
			throw new ValidationPennyException("DeliveryTimeSlot must be 1..7.");
		}

		var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(userIdStr, out var userId)) {
			return this.Unauthorized();
		}

		await this._purchaseService.PurchaseWonProductAsync(userId, request);
		return this.Ok();
	}

	[HttpGet("status/{auctionItemId:int}")]
	public async Task<ActionResult<PurchaseStatusDto>> GetStatusAsync(int auctionItemId) {
		var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(userIdStr, out var userId)) {
			return this.Unauthorized();
		}

		var status = await this._purchaseService.GetPurchaseStatusAsync(userId, auctionItemId);
		return this.Ok(status);
	}
}