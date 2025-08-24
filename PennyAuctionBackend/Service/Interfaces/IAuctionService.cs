using PennyAuctionBackend.Dtos.AuctionItem;

namespace PennyAuctionBackend.Service.Interfaces;

public interface IAuctionService {
	public Task<SearchAuctionItemsResponse> SearchAsync(int categoryId);
}