using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PennyAuctionBackend.Data;
using PennyAuctionBackend.Dtos.Payments;
using PennyAuctionBackend.Dtos.Points;
using PennyAuctionBackend.Exceptions;
using PennyAuctionBackend.Models;
using PennyAuctionBackend.Services.Interfaces;
using PennyAuctionBackend.Utils.Attributes;

namespace PennyAuctionBackend.Services.Implementations;

[AddScoped]
public class PointService(PennyDbContext db, IConfiguration config) : IPointService {
	private readonly IConfiguration _config = config;
	private readonly PennyDbContext _db = db;

	public async Task<IReadOnlyCollection<PointPlanDto>> GetPlansAsync() {
		var plans = await this._db.PointPlans
			.OrderBy(p => p.Price)
			.Select(p => new PointPlanDto { Id = p.Id, Name = p.Name, Points = p.Points, Price = p.Price })
			.ToListAsync();
		return plans;
	}

	public async Task<RedeemDepositResponse> PurchaseAsync(int userId, RedeemDepositRequest request) {
		if (string.IsNullOrWhiteSpace(request.DepositToken)) {
			throw new ValidationPennyException("DepositToken required");
		}

		var pointPlan = await this._db.PointPlans.FirstOrDefaultAsync(p => p.Id == request.PointPlanId);
		if (pointPlan == null) {
			throw new ValidationPennyException("Invalid point plan");
		}

		var extKey = this._config["ExternalPaymentJwt:Key"] ?? throw new ConfigurationPennyException("ExternalPaymentJwt:Key configuration is missing.");
		var extIssuer = this._config["ExternalPaymentJwt:Issuer"] ?? throw new ConfigurationPennyException("ExternalPaymentJwt:Issuer configuration is missing.");
		var handler = new JwtSecurityTokenHandler();
		var validationParams = new TokenValidationParameters {
			ValidateIssuer = true,
			ValidIssuer = extIssuer,
			ValidateAudience = false,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(extKey)),
			ClockSkew = TimeSpan.FromSeconds(5)
		};

		ClaimsPrincipal extPrincipal;
		try {
			extPrincipal = handler.ValidateToken(request.DepositToken, validationParams, out _);
		} catch {
			throw new ValidationPennyException("Invalid deposit token");
		}

		var claims = extPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
		if (!claims.TryGetValue("tokenType", out var tokenType) || tokenType != "deposit") {
			throw new ValidationPennyException("Invalid token type");
		}

		if (!claims.TryGetValue("provider", out var provider) || string.IsNullOrEmpty(provider)) {
			throw new ValidationPennyException("Invalid provider");
		}

		if (!claims.TryGetValue("amount", out var amountStr) || !int.TryParse(amountStr, out var amount) || amount <= 0) {
			throw new ValidationPennyException("Invalid amount");
		}

		// 価格整合性チェック
		if (amount != pointPlan.Price) {
			throw new ValidationPennyException("Amount does not match plan price");
		}

		claims.TryGetValue(JwtRegisteredClaimNames.Jti, out var jti);
		if (string.IsNullOrEmpty(jti)) {
			throw new ValidationPennyException("Missing JTI");
		}

		var used = await this._db.PointPurchases.AnyAsync(p => p.ExternalTokenJti == jti);
		if (used) {
			throw new ValidationPennyException("Token already used");
		}

		await using var transaction = await this._db.Database.BeginTransactionAsync();
		var user = await this._db.Users
			.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE")
			.AsTracking()
			.FirstOrDefaultAsync();
		if (user == null) {
			throw new ValidationPennyException("User not found");
		}

		var purchase = new PointPurchase {
			UserId = user.Id,
			Points = pointPlan.Points,
			AmountPaid = amount,
			Note = $"plan:{pointPlan.Name}",
			ExternalTokenJti = jti
		};
		this._db.PointPurchases.Add(purchase);

		// 単価
		var unitPrice = pointPlan.Price / (decimal)pointPlan.Points;
		var lot = new PointBalanceLot { UserId = user.Id, UnitPrice = unitPrice, QuantityRemaining = pointPlan.Points };
		this._db.PointBalanceLots.Add(lot);

		user.PointBalance += pointPlan.Points;

		var pt = new PointTransaction {
			UserId = user.Id,
			Type = PointTransactionType.Purchase,
			TotalAmount = pointPlan.Points,
			BalanceAfter = user.PointBalance,
			PointPurchase = purchase,
			Note = $"plan:{pointPlan.Name}"
		};
		var entry = new PointTransactionEntry {
			Transaction = pt,
			Quantity = pointPlan.Points,
			PointBalanceLot = lot,
			UnitPrice = lot.UnitPrice,
			TotalPrice = pointPlan.Price
		};
		pt.Entries.Add(entry);
		lot.PointTransactionEntries.Add(entry);
		this._db.PointTransactions.Add(pt);
		this._db.PointTransactionEntries.Add(entry);

		// 通知追加
		this._db.Notifications.Add(new() {
			UserId = user.Id,
			Category = "purchase",
			Title = "ポイントチャージ完了",
			Message = $"{pointPlan.Points}pt をチャージしました (¥{pointPlan.Price})",
			IsRead = false
		});

		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();

		return new() { AddedPoints = pointPlan.Points, NewBalance = user.PointBalance };
	}
}