using PennyAuctionBackend.Dtos.Payments;
using PennyAuctionBackend.Dtos.Points;

namespace PennyAuctionBackend.Services.Interfaces;

public interface IPointService {
	public Task<IReadOnlyCollection<PointPlanDto>> GetPlansAsync();
	public Task<RedeemDepositResponse> PurchaseAsync(int userId, RedeemDepositRequest request);
}