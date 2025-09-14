using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using CaliphAuctionBackend.Data;
using CaliphAuctionBackend.Dtos.AuctionItem;
using CaliphAuctionBackend.Dtos.MyPage;
using CaliphAuctionBackend.Exceptions;
using CaliphAuctionBackend.Models;
using CaliphAuctionBackend.Services.Interfaces;
using CaliphAuctionBackend.Utils.Attributes;

namespace CaliphAuctionBackend.Services.Implementations;

[AddScoped]
public class MyPageService(CaliphDbContext db) : IMyPageService {
	private readonly CaliphDbContext _db = db;

	public async Task<MyPageSummaryDto> GetSummaryAsync(int userId) {
		var user = await this._db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null) {
			throw new ValidationCaliphException("User not found.");
		}

		var pointPurchaseSpent = await this._db.PointPurchases
			.Where(p => p.UserId == userId)
			.SumAsync(p => (int?)p.AmountPaid) ?? 0;

		var auctionItemDepositSpent = await this._db.AuctionItemPurchases
			.Where(p => p.UserId == userId)
			.SumAsync(p => (int?)p.DepositAmount) ?? 0;

		var totalSpent = auctionItemDepositSpent + pointPurchaseSpent;

		var notifications = await this._db.Notifications
			.Where(n => n.UserId == null || n.UserId == userId)
			.OrderByDescending(n => n.CreatedAt)
			.Take(10)
			.Select(n => new NotificationDto {
				Id = n.Id,
				Category = n.Category,
				Title = n.Title,
				Message = n.Message,
				CreatedAt = n.CreatedAt,
				IsRead = n.IsRead
			})
			.ToListAsync();

		return new() { PointBalance = user.PointBalance, TotalSpentAmount = totalSpent, Notifications = notifications };
	}

	public async Task<SearchAuctionItemsResponse> GetBiddingItemsAsync(int userId, int? limit) {
		return await this.GetAuctionItemsWithPredicateAsync(limit, ai => ai.Status == AuctionStatus.Active && ai.Bids.Any(b => b.UserId == userId));
	}

	public async Task<SearchAuctionItemsResponse> GetWonItemsAsync(int userId, int? limit) {
		return await this.GetAuctionItemsWithPredicateAsync(limit, ai => ai.Status == AuctionStatus.Ended && ai.CurrentHighestBidUserId == userId);
	}

	private async Task<SearchAuctionItemsResponse> GetAuctionItemsWithPredicateAsync(int? limit, Expression<Func<AuctionItem, bool>> predicate) {
		IQueryable<AuctionItem> query = this._db.AuctionItems
			.AsNoTracking()
			.Where(predicate)
			.OrderByDescending(ai => ai.EndTime);

		var total = await query.CountAsync();
		if (limit is { } intLimit) {
			query = query.Take(intLimit);
		}

		var items = await query
			.Include(ai => ai.CurrentHighestBidUser)
			.Include(ai => ai.Purchase)
			.Select(ai => new AuctionItemSummaryDto {
				Id = ai.Id,
				Name = ai.Name,
				ThumbnailImageUrl = ai.ThumbnailImageUrl,
				CurrentPrice = ai.CurrentPrice,
				BidIncrement = ai.BidIncrement,
				BidPointCost = ai.BidPointCost,
				EndTime = ai.EndTime,
				CategoryId = ai.CategoryId,
				Status = ai.Status,
				CurrentHighestBidUserId = ai.CurrentHighestBidUserId,
				CurrentHighestBidUserName = ai.CurrentHighestBidUser != null ? ai.CurrentHighestBidUser.Username : null,
				Purchased = ai.Purchase != null
			})
			.ToListAsync();

		return new() { Items = items, TotalCount = total, ServerTimeUtc = DateTime.UtcNow };
	}
}