using System.Net;
using Microsoft.EntityFrameworkCore;
using PennyAuctionBackend.Data;
using PennyAuctionBackend.Dtos.AuctionItem;
using PennyAuctionBackend.Exceptions;
using PennyAuctionBackend.Models;
using PennyAuctionBackend.Service.Interfaces;
using PennyAuctionBackend.Utils.Attributes;

namespace PennyAuctionBackend.Service.Implementations;

/// <summary>
///     オークションに関する読み取り系のサービス。
///     カテゴリ別のアイテム検索など、クエリ機能を提供します。
/// </summary>
[AddScoped]
public class AuctionService(PennyDbContext dbContext, IConfiguration configuration) : IAuctionService {
	private readonly IConfiguration _configuration = configuration;
	private readonly PennyDbContext _db = dbContext;

	/// <inheritdoc />
	public async Task<SearchAuctionItemsResponse> SearchAsync(int categoryId) {
		var baseQuery = this._db.AuctionItems
			.AsNoTracking()
			.Where(x => x.CategoryId == categoryId && x.Status == AuctionStatus.Active);

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
				CurrentUserName = x.CurrentHighestBidUser != null ? x.CurrentHighestBidUser.Username : null
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
		await using var transaction = await this._db.Database.BeginTransactionAsync();

		var item = await this._db.AuctionItems
			.Include(x => x.Bids)
			.FirstOrDefaultAsync(x => x.Id == request.AuctionItemId);

		if (item is null) {
			throw new ValidationPennyException("Auction item not found.");
		}

		if (item.Status != AuctionStatus.Active) {
			throw new ValidationPennyException("Auction is not active.");
		}

		var userExists = await this._db.Users.AnyAsync(u => u.Id == userId);
		if (!userExists) {
			throw new ValidationPennyException("User not found.");
		}

		this._db.Bids.Add(new() {
			AuctionItemId = item.Id,
			AuctionItem = item,
			UserId = userId,
			BidAmount = request.BidAmount,
			BidTime = DateTime.UtcNow
		});

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
	}
}