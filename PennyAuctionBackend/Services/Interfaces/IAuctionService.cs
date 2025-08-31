using PennyAuctionBackend.Dtos.AuctionItem;

namespace PennyAuctionBackend.Services.Interfaces;

public interface IAuctionService {
	public Task<SearchAuctionItemsResponse> SearchAsync(int? categoryId);
	public Task<AuctionItemDetailDto> GetDetailAsync(int id);
	public Task PlaceBidAsync(int userId, PlaceBidRequest request);
}