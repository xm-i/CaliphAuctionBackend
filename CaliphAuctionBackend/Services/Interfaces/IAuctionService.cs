using CaliphAuctionBackend.Dtos.AuctionItem;

namespace CaliphAuctionBackend.Services.Interfaces;

public interface IAuctionService {
	public Task<SearchAuctionItemsResponse> SearchAsync(int? categoryId);
	public Task<AuctionItemDetailDto> GetDetailAsync(int id);
	public Task PlaceBidAsync(int userId, PlaceBidRequest request, string ipAddress);
	public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync();
}