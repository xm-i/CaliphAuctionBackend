using CaliphAuctionBackend.Dtos.Purchases;

namespace CaliphAuctionBackend.Services.Interfaces;

public interface IPurchaseService {
	public Task PurchaseWonProductAsync(int userId, PurchaseWonProductRequest request);
	public Task<PurchaseStatusDto> GetPurchaseStatusAsync(int userId, int auctionItemId);
}