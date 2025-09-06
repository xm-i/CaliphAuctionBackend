using Microsoft.EntityFrameworkCore;
using PennyAuctionBackend.Data;
using PennyAuctionBackend.Dtos.MyPage;
using PennyAuctionBackend.Exceptions;
using PennyAuctionBackend.Services.Interfaces;
using PennyAuctionBackend.Utils.Attributes;

namespace PennyAuctionBackend.Services.Implementations;

[AddScoped]
public class MyPageService(PennyDbContext db) : IMyPageService {
	private readonly PennyDbContext _db = db;

	public async Task<MyPageSummaryDto> GetSummaryAsync(int userId) {
		var user = await this._db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null) {
			throw new ValidationPennyException("User not found.");
		}

		var totalSpent = await this._db.PointPurchases
			.Where(p => p.UserId == userId)
			.SumAsync(p => (int?)p.AmountPaid) ?? 0;

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
}