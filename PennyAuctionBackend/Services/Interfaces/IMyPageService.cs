using PennyAuctionBackend.Dtos.AuctionItem;
using PennyAuctionBackend.Dtos.MyPage;

namespace PennyAuctionBackend.Services.Interfaces;

public interface IMyPageService {
	public Task<MyPageSummaryDto> GetSummaryAsync(int userId);

	/// <summary>
	///     現在入札中(入札履歴があり、まだ終了していない)のオークション一覧
	/// </summary>
	/// <param name="userId">ユーザーID</param>
	/// <param name="limit">取得件数上限</param>
	public Task<SearchAuctionItemsResponse> GetBiddingItemsAsync(int userId, int? limit);

	/// <summary>
	///     落札済み(終了し、最高入札者が自分)のオークション一覧
	/// </summary>
	/// <param name="userId">ユーザー</param>
	/// <param name="limit">取得件数上限</param>
	public Task<SearchAuctionItemsResponse> GetWonItemsAsync(int userId, int? limit);
}