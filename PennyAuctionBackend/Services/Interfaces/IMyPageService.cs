using PennyAuctionBackend.Dtos.MyPage;

namespace PennyAuctionBackend.Services.Interfaces;

public interface IMyPageService {
	public Task<MyPageSummaryDto> GetSummaryAsync(int userId);
}