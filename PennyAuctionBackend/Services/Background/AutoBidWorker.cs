using Microsoft.EntityFrameworkCore;
using PennyAuctionBackend.Data;
using PennyAuctionBackend.Exceptions;
using PennyAuctionBackend.Models;
using PennyAuctionBackend.Services.Infrastructure;
using PennyAuctionBackend.Services.Interfaces;

namespace PennyAuctionBackend.Services.Background;

public class AutoBidWorker(
	int auctionItemId,
	IServiceScopeFactory scopeFactory,
	ILogger<AutoBidWorker> logger) {
	private readonly int _auctionItemId = auctionItemId;
	private readonly ILogger<AutoBidWorker> _logger = logger;
	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

	public async Task RunAsync(CancellationToken ct) {
		this._logger.LogInformation("AutoBidWorker started for Item {ItemId}", this._auctionItemId);

		try {
			using var scope = this._scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<PennyDbContext>();
			var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();

			// 最新状態取得
			var initial = await db.AuctionItems
				.AsNoTracking()
				.Where(x => x.Id == this._auctionItemId)
				.Select(x => new {
					x.Id,
					x.Status,
					x.EndTime,
					x.CurrentPrice,
					x.MinimumPrice,
					x.BidIncrement,
					x.CurrentHighestBidUserId
				})
				.FirstOrDefaultAsync(ct);

			if (initial is null) {
				this._logger.LogInformation("Item {ItemId} no longer exists. Stop worker.", this._auctionItemId);
				return;
			}

			if (initial.Status != AuctionStatus.Active) {
				this._logger.LogInformation("Item {ItemId} not active. Stop worker.", this._auctionItemId);
				return;
			}

			if (initial.CurrentPrice >= initial.MinimumPrice) {
				this._logger.LogInformation("Item {ItemId} reached MinimumPrice. Stop worker.", this._auctionItemId);
				return;
			}

			// 次回入札タイミングを決定して待機
			var slackSec = Random.Shared.Next(5, 20);
			var targetAt = initial.EndTime.AddSeconds(-slackSec);
			var now = DateTime.UtcNow;
			if (targetAt > now) {
				var delay = targetAt - now;
				await Task.Delay(delay, ct);
			}

			// 最新状態を再取得
			var latest = await db.AuctionItems
				.AsNoTracking()
				.Where(x => x.Id == this._auctionItemId)
				.Select(x => new {
					x.Id,
					x.Status,
					x.EndTime,
					x.CurrentPrice,
					x.MinimumPrice,
					x.BidIncrement,
					x.CurrentHighestBidUserId
				})
				.FirstOrDefaultAsync(ct);

			if (latest is null) {
				this._logger.LogWarning("Item {ItemId} no longer exists.", this._auctionItemId);
				return;
			}

			// 状態が変わっていたら（時間延長されていたら）今回は終了し、Coordinatorに任せる
			if (latest.EndTime != initial.EndTime) {
				this._logger.LogDebug("Item {ItemId} state changed before bid. EndTime/Price updated.", this._auctionItemId);
				return;
			}

			// Botユーザー選定（現最高入札者は除外）
			var botUserId = BotUserCache.GetRandomBotUserId(latest.CurrentHighestBidUserId);
			if (botUserId is null) {
				this._logger.LogWarning("No available bot users for Item {ItemId}", this._auctionItemId);
				return;
			}

			try {
				await auctionService.PlaceBidAsync(botUserId.Value, new() { AuctionItemId = this._auctionItemId, BidAmount = latest.CurrentPrice + latest.BidIncrement });
			} catch (ValidationPennyException ex) {
				this._logger.LogDebug(ex, "Validation during autobid for Item {ItemId}", this._auctionItemId);
			} catch (Exception ex) {
				this._logger.LogError(ex, "Unexpected error during autobid for Item {ItemId}", this._auctionItemId);
			}
		} catch (OperationCanceledException) {
			// 正常停止
		} catch (Exception ex) {
			this._logger.LogError(ex, "AutoBidWorker error for Item {ItemId}", this._auctionItemId);
		} finally {
			this._logger.LogInformation("AutoBidWorker stopped for Item {ItemId}", this._auctionItemId);
		}
	}
}