using PennyAuctionBackend.Dtos.Purchases;

namespace PennyAuctionBackend.Services.Interfaces;

public interface IPurchaseService {
	public Task PurchaseWonProductAsync(int userId, PurchaseWonProductRequest request);
	public Task<PurchaseStatusDto> GetPurchaseStatusAsync(int userId, int auctionItemId);
}