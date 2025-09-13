using System.Data;
using System.Net;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PennyAuctionBackend.Data;
using PennyAuctionBackend.Dtos.AuctionItem;
using PennyAuctionBackend.Dtos.Realtime;
using PennyAuctionBackend.Exceptions;
using PennyAuctionBackend.Hubs;
using PennyAuctionBackend.Models;
using PennyAuctionBackend.Services.Interfaces;
using PennyAuctionBackend.Utils.Attributes;

namespace PennyAuctionBackend.Services.Implementations;

/// <summary>
///     オークションに関する読み取り系のサービス。
///     カテゴリ別のアイテム検索など、クエリ機能を提供します。
/// </summary>
[AddScoped]
public class AuctionService(PennyDbContext dbContext, IConfiguration configuration, IHubContext<AuctionHub, IAuctionClient> hubContext) : IAuctionService {
	private readonly IConfiguration _configuration = configuration;
	private readonly PennyDbContext _db = dbContext;
	private readonly IHubContext<AuctionHub, IAuctionClient> _hub = hubContext;

	/// <inheritdoc />
	public async Task<SearchAuctionItemsResponse> SearchAsync(int? categoryId) {
		var baseQuery = this._db.AuctionItems
			.AsNoTracking()
			.Where(x => x.Status == AuctionStatus.Active);

		if (categoryId is { } cid) {
			baseQuery = baseQuery.Where(x => x.CategoryId == cid);
		}

		var total = await baseQuery.CountAsync();

		var items = await baseQuery
			.Include(x => x.CurrentHighestBidUser)
			.OrderBy(x => x.EndTime)
			.Take(10)
			.Select(x => new AuctionItemSummaryDto {
				Id = x.Id,
				Name = x.Name,
				ThumbnailImageUrl = x.ThumbnailImageUrl,
				CurrentPrice = x.CurrentPrice,
				BidIncrement = x.BidIncrement,
				BidPointCost = x.BidPointCost,
				EndTime = x.EndTime,
				CategoryId = x.CategoryId,
				Status = x.Status,
				CurrentHighestBidUserId = x.CurrentHighestBidUserId,
				CurrentHighestBidUserName = x.CurrentHighestBidUser != null ? x.CurrentHighestBidUser.Username : null,
				Purchased = false
			})
			.ToListAsync();

		return new() { Items = items, TotalCount = total };
	}

	public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync() {
		var categories = await this._db.AuctionItemCategories
			.AsNoTracking()
			.OrderBy(c => c.Id)
			.Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, ActiveItemCount = c.AuctionItems.Count(ai => ai.Status == AuctionStatus.Active) })
			.ToListAsync();
		return categories;
	}

	/// <summary>
	///     商品IDで詳細を取得します。
	/// </summary>
	/// <param name="id">商品ID</param>
	/// <returns>商品情報</returns>
	public async Task<AuctionItemDetailDto> GetDetailAsync(int id) {
		var item = await this._db.AuctionItems
			.AsNoTracking()
			.Include(x => x.Category)
			.Include(x => x.CurrentHighestBidUser)
			.Where(x => x.Id == id)
			.Select(x => new AuctionItemDetailDto {
				Id = x.Id,
				Name = x.Name,
				Description = x.Description,
				ThumbnailImageUrl = x.ThumbnailImageUrl,
				ImageUrl = x.ImageUrl,
				OriginalPrice = x.OriginalPrice,
				StartingBid = x.StartingBid,
				BidIncrement = x.BidIncrement,
				BidPointCost = x.BidPointCost,
				CurrentPrice = x.CurrentPrice,
				EndTime = x.EndTime,
				CategoryId = x.CategoryId,
				CategoryName = x.Category.Name,
				CurrentHighestBidUserId = x.CurrentHighestBidUserId,
				CurrentHighestBidUserName = x.CurrentHighestBidUser != null ? x.CurrentHighestBidUser.Username : null,
				BidCount = x.Bids.Count,
				Status = x.Status,
				BidHistories = x.Bids
					.OrderByDescending(b => b.BidTime)
					.Select(b => new BidHistoryDto { UserId = b.UserId, Username = b.User.Username, BidAmount = b.BidAmount, BidTime = b.BidTime })
					.Take(20)
					.ToList()
			})
			.FirstOrDefaultAsync();

		if (item is null) {
			throw new PennyException("Auction item not found", null, HttpStatusCode.NotFound);
		}

		return item;
	}

	/// <summary>
	///     入札を行い、ポイントを消費する。
	/// </summary>
	public async Task PlaceBidAsync(int userId, PlaceBidRequest request) {
		await using var transaction = await this._db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

		// アイテム行ロック
		var item = await this._db.AuctionItems
			.FromSqlInterpolated($"SELECT * FROM \"AuctionItems\" WHERE \"Id\" = {request.AuctionItemId} FOR UPDATE")
			.AsTracking()
			.FirstOrDefaultAsync();
		if (item is null) {
			throw new ValidationPennyException("Auction item not found.");
		}

		if (item.Status != AuctionStatus.Active) {
			throw new ValidationPennyException("Auction is not active.");
		}

		// すでに現在の最高入札者が自分の場合、入札不可
		if (item.CurrentHighestBidUserId.HasValue && item.CurrentHighestBidUserId.Value == userId) {
			throw new ValidationPennyException("You are already the highest bidder.");
		}

		// ユーザー行ロック (残高整合性確保)
		var user = await this._db.Users
			.FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE")
			.AsTracking()
			.FirstOrDefaultAsync();
		if (user is null) {
			throw new ValidationPennyException("User not found.");
		}

		// 次の入札金額検証
		var expectedNextAmount = item.CurrentPrice + item.BidIncrement;
		if (request.BidAmount != expectedNextAmount) {
			throw new ValidationPennyException($"Invalid bid amount. The next bid must be exactly {expectedNextAmount}.");
		}

		// 入札エンティティ作成
		var bid = new Bid {
			AuctionItemId = item.Id,
			AuctionItem = item,
			UserId = userId,
			BidAmount = request.BidAmount,
			BidTime = DateTime.UtcNow
		};
		// BOTでなければポイント消費し、トータルコスト積み上げ
		if (!user.IsBotUser) {
			var totalCost = await this.ConsumeBidPointsAsync(user, bid, item.BidPointCost);
			item.TotalBidCost += totalCost;
		}

		this._db.Bids.Add(bid);


		// 残り時間の延長判定
		var nullableMinimumEndTimeSeconds = this._configuration.GetValue<int?>("Auction:MinimumEndTimeSeconds", null);
		if (nullableMinimumEndTimeSeconds is not { } minimumEndTimeSeconds) {
			throw new ConfigurationPennyException("Auction:MinimumEndTimeSeconds configuration is missing.");
		}

		var minimumEndTime = DateTime.UtcNow.AddSeconds(minimumEndTimeSeconds);
		item.CurrentPrice = request.BidAmount;
		item.CurrentHighestBidUserId = userId;
		if (item.EndTime < minimumEndTime) {
			item.EndTime = minimumEndTime;
		}

		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();

		var update = new BidUpdateDto {
			AuctionItemId = item.Id,
			CurrentPrice = item.CurrentPrice,
			EndTime = item.EndTime,
			BidId = bid.Id,
			BidTime = bid.BidTime,
			CurrentHighestBidUserId = userId,
			CurrentHighestBidUserName = user.Username
		};

		await this._hub.Clients.Group(AuctionHub.BuildGroupName(item.Id)).ReceiveBidUpdate(update);
	}

	/// <summary>
	///     入札1回分のポイントを消費し、Spendトランザクションと明細を生成。
	/// </summary>
	/// <param name="user">入札ユーザー</param>
	/// <param name="bid">入札エンティティ</param>
	/// <param name="perBidCost">消費ポイント</param>
	/// <returns>消費コインに対応する利用金額</returns>
	private async Task<int> ConsumeBidPointsAsync(User user, Bid bid, int perBidCost) {
		if (user.PointBalance < perBidCost) {
			throw new ValidationPennyException("Insufficient points.");
		}

		var remaining = perBidCost;
		var lots = await this._db.PointBalanceLots
			.Where(l => l.UserId == user.Id && l.QuantityRemaining > 0)
			.OrderBy(l => l.UnitPrice)
			.ToListAsync();

		var entries = new List<PointTransactionEntry>();
		foreach (var lot in lots) {
			if (remaining <= 0) {
				break;
			}

			var take = Math.Min(lot.QuantityRemaining, remaining);
			lot.QuantityRemaining -= take;
			remaining -= take;
			var totalPrice = (int)(lot.UnitPrice * take);
			entries.Add(new() { PointBalanceLotId = lot.Id, Quantity = -take, UnitPrice = lot.UnitPrice, TotalPrice = totalPrice });
		}

		user.PointBalance -= perBidCost;

		var spendTx = new PointTransaction {
			UserId = user.Id,
			Type = PointTransactionType.Spend,
			TotalAmount = -perBidCost,
			BalanceAfter = user.PointBalance,
			Bid = bid,
			Note = "Bid spend",
			Entries = entries
		};
		this._db.PointTransactions.Add(spendTx);

		return entries.Sum(x => x.TotalPrice);
	}
}