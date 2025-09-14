using CaliphAuctionBackend.Dtos.Payments;
using CaliphAuctionBackend.Dtos.Points;

namespace CaliphAuctionBackend.Services.Interfaces;

public interface IPointService {
	public Task<IReadOnlyCollection<PointPlanDto>> GetPlansAsync();
	public Task<RedeemDepositResponse> PurchaseAsync(int userId, RedeemDepositRequest request);
}