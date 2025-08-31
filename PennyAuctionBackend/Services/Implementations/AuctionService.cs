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
				EndTime = x.EndTime,
				CategoryId = x.CategoryId,
				CurrentHighestBidUserId = x.CurrentHighestBidUserId,
				CurrentHighestBidUserName = x.CurrentHighestBidUser != null ? x.CurrentHighestBidUser.Username : null
			})
			.ToListAsync();

		return new() { Items = items, TotalCount = total };
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
				CurrentPrice = x.CurrentPrice,
				EndTime = x.EndTime,
				CategoryId = x.CategoryId,
				CategoryName = x.Category.Name,
				CurrentHighestBidUserId = x.CurrentHighestBidUserId,
				CurrentHighestBidUserName = x.CurrentHighestBidUser != null ? x.CurrentHighestBidUser.Username : null,
				BidCount = x.Bids.Count,
				Status = (int)x.Status,
				BidHistories = x.Bids
					.OrderByDescending(b => b.BidTime)
					.Select(b => new BidHistoryDto { UserId = b.UserId, Username = b.User.Username, BidAmount = b.BidAmount, BidTime = b.BidTime })
					.ToList()
			})
			.FirstOrDefaultAsync();

		if (item is null) {
			throw new PennyException("Auction item not found", null, HttpStatusCode.NotFound);
		}

		return item;
	}

	/// <summary>
	///     入札を行う。
	/// </summary>
	/// <param name="userId">入札ユーザー ID</param>
	/// <param name="request">入札情報</param>
	public async Task PlaceBidAsync(int userId, PlaceBidRequest request) {
		await using var transaction = await this._db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

		var itemQuery = this._db.AuctionItems
			.FromSqlInterpolated($"SELECT * FROM \"AuctionItems\" WHERE \"Id\" = {request.AuctionItemId} FOR UPDATE")
			.AsTracking();
		var item = await itemQuery.FirstOrDefaultAsync();

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

		var userExists = await this._db.Users.AnyAsync(u => u.Id == userId);
		if (!userExists) {
			throw new ValidationPennyException("User not found.");
		}

		// 金額チェック：現在価格 + 入札幅 であること
		var expectedNextAmount = item.CurrentPrice + item.BidIncrement;
		if (request.BidAmount != expectedNextAmount) {
			throw new ValidationPennyException($"Invalid bid amount. The next bid must be exactly {expectedNextAmount}.");
		}

		var bid = new Bid {
			AuctionItemId = item.Id,
			AuctionItem = item,
			UserId = userId,
			BidAmount = request.BidAmount,
			BidTime = DateTime.UtcNow
		};
		this._db.Bids.Add(bid);

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

		var username = await this._db.Users
			.Where(u => u.Id == userId)
			.Select(u => u.Username)
			.FirstAsync();

		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();

		var update = new BidUpdateDto {
			AuctionItemId = item.Id,
			CurrentPrice = item.CurrentPrice,
			EndTime = item.EndTime,
			BidId = bid.Id,
			BidTime = bid.BidTime,
			CurrentHighestBidUserId = userId,
			CurrentHighestBidUserName = username
		};

		await this._hub.Clients.Group(AuctionHub.BuildGroupName(item.Id)).ReceiveBidUpdate(update);
	}
}