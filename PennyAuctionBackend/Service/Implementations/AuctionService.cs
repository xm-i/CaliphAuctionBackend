using Microsoft.EntityFrameworkCore;
using PennyAuctionBackend.Data;
using PennyAuctionBackend.Dtos.AuctionItem;
using PennyAuctionBackend.Models;
using PennyAuctionBackend.Service.Interfaces;
using PennyAuctionBackend.Utils.Attributes;

namespace PennyAuctionBackend.Service.Implementations;

[AddScoped]
public class AuctionService(PennyDbContext dbContext) : IAuctionService {
	private readonly PennyDbContext _db = dbContext;

	/// <summary>
	///     指定した検索条件に該当するオークションアイテムを取得する。
	/// </summary>
	/// <param name="categoryId">検索対象のカテゴリ ID（正の整数）</param>
	/// <returns>
	///     <see cref="SearchAuctionItemsResponse" /><br />
	///     検索結果
	/// </returns>
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
}