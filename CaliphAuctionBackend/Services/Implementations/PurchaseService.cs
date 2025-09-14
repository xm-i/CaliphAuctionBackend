using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CaliphAuctionBackend.Data;
using CaliphAuctionBackend.Dtos.Purchases;
using CaliphAuctionBackend.Exceptions;
using CaliphAuctionBackend.Models;
using CaliphAuctionBackend.Services.Interfaces;
using CaliphAuctionBackend.Utils.Attributes;

namespace CaliphAuctionBackend.Services.Implementations;

[AddScoped]
public class PurchaseService(CaliphDbContext db, IConfiguration config) : IPurchaseService {
	private readonly IConfiguration _config = config;
	private readonly CaliphDbContext _db = db;

	public async Task PurchaseWonProductAsync(int userId, PurchaseWonProductRequest request) {
		var auctionItem = await this._db.AuctionItems
			.Where(ai => ai.Id == request.AuctionId && ai.Status == AuctionStatus.Ended && ai.CurrentHighestBidUserId == userId)
			.Include(ai => ai.Product)
			.FirstOrDefaultAsync();
		if (auctionItem == null) {
			throw new ValidationCaliphException("No won auction item for this auction id.");
		}

		var alreadyPurchased = await this._db.AuctionItemPurchases
			.AsNoTracking()
			.AnyAsync(p => p.AuctionItemId == auctionItem.Id);
		if (alreadyPurchased) {
			throw new ValidationCaliphException("Purchase already exists for this auction item.");
		}

		var key = this._config["ExternalPaymentJwt:Key"] ?? throw new ConfigurationCaliphException("ExternalPaymentJwt:Key missing");
		var issuer = this._config["ExternalPaymentJwt:Issuer"] ?? throw new ConfigurationCaliphException("ExternalPaymentJwt:Issuer missing");

		var tokenHandler = new JwtSecurityTokenHandler();
		var validationParams = new TokenValidationParameters {
			ValidateIssuer = true,
			ValidIssuer = issuer,
			ValidateAudience = false,
			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromMinutes(1),
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
		};

		ClaimsPrincipal principal;
		JwtSecurityToken rawToken;
		try {
			principal = tokenHandler.ValidateToken(request.DepositToken, validationParams, out var validatedToken);
			rawToken = (JwtSecurityToken)validatedToken;
		} catch {
			throw new ValidationCaliphException("Invalid deposit token.");
		}

		string GetClaim(string type) {
			return principal.Claims.FirstOrDefault(c => c.Type == type)?.Value
			       ?? throw new ValidationCaliphException($"Deposit token missing claim: {type}");
		}

		var tokenType = GetClaim("tokenType");
		if (tokenType != "deposit") {
			throw new ValidationCaliphException("Invalid deposit token type.");
		}

		var amountStr = GetClaim("amount");
		if (!int.TryParse(amountStr, out var depositAmount) || depositAmount <= 0) {
			throw new ValidationCaliphException("Invalid deposit amount.");
		}

		// 商品金額(最終落札価格)と一致するかチェック
		if (depositAmount != (int)auctionItem.CurrentPrice) {
			throw new ValidationCaliphException("Deposit amount does not match final auction price.");
		}

		var jti = rawToken.Id;
		var duplicateJti = await this._db.AuctionItemPurchases.AnyAsync(p => p.DepositTokenJti == jti);
		if (duplicateJti) {
			throw new ValidationCaliphException("Deposit token already used.");
		}

		var purchase = new AuctionItemPurchase {
			UserId = userId,
			AuctionItemId = auctionItem.Id,
			Prefecture = request.Prefecture,
			City = request.City,
			AddressLine1 = request.AddressLine1,
			AddressLine2 = request.AddressLine2,
			DeliveryDate = request.DeliveryDate,
			DeliveryTimeSlot = request.DeliveryTimeSlot,
			ShippingCarrier = request.ShippingCarrier,
			DepositTokenJti = jti,
			DepositAmount = depositAmount
		};

		this._db.AuctionItemPurchases.Add(purchase);
		await this._db.SaveChangesAsync();
	}

	public async Task<PurchaseStatusDto> GetPurchaseStatusAsync(int userId, int auctionItemId) {
		var item = await this._db.AuctionItems
			.AsNoTracking()
			.FirstOrDefaultAsync(ai => ai.Id == auctionItemId);
		if (item == null) {
			throw new ValidationCaliphException("Auction item not found.");
		}

		var isWinner = item.Status == AuctionStatus.Ended && item.CurrentHighestBidUserId == userId;
		if (!isWinner) {
			throw new ValidationCaliphException("Not authorized to view purchase status for this auction item.");
		}

		var purchase = await this._db.AuctionItemPurchases.AsNoTracking().FirstOrDefaultAsync(p => p.AuctionItemId == auctionItemId);

		return new() {
			AuctionItemId = auctionItemId,
			Purchased = purchase != null,
			DeliveryDate = purchase?.DeliveryDate,
			DeliveryTimeSlot = purchase?.DeliveryTimeSlot,
			ShippingCarrier = purchase?.ShippingCarrier,
			Prefecture = purchase?.Prefecture,
			City = purchase?.City,
			AddressLine1 = purchase?.AddressLine1,
			AddressLine2 = purchase?.AddressLine2
		};
	}
}