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
public class AuctionService(PennyDbContext dbContext) : IAuctionService {
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
}