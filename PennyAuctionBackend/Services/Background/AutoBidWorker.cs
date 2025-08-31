using System.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PennyAuctionBackend.Data;
using PennyAuctionBackend.Dtos.Realtime;
using PennyAuctionBackend.Exceptions;
using PennyAuctionBackend.Hubs;
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

			// 最新状態取得
			var initial = await db.AuctionItems
				.AsNoTracking()
				.Where(x => x.Id == this._auctionItemId)
				.FirstOrDefaultAsync(ct);

			if (initial is null) {
				this._logger.LogInformation("Item {ItemId} no longer exists. Stop worker.", this._auctionItemId);
				return;
			}

			if (initial.Status != AuctionStatus.Active) {
				this._logger.LogInformation("Item {ItemId} not active. Stop worker.", this._auctionItemId);
				return;
			}

			//　最低価格に達していれば終了処理へ
			if (initial.CurrentPrice >= initial.MinimumPrice) {
				await this.FinalizeAsync(scope, db, initial, ct);
			} else {
				await this.BotBidAsync(scope, db, initial, ct);
			}
		} catch (OperationCanceledException) {
			// 正常停止
		} catch (Exception ex) {
			this._logger.LogError(ex, "AutoBidWorker error for Item {ItemId}", this._auctionItemId);
		} finally {
			this._logger.LogInformation("AutoBidWorker stopped for Item {ItemId}", this._auctionItemId);
		}
	}

	/// <summary>
	///     Bot入札処理
	/// </summary>
	/// <param name="scope">Scope</param>
	/// <param name="db">DbContext</param>
	/// <param name="initial">初回取得値</param>
	/// <param name="ct">CancellationToken</param>
	private async Task BotBidAsync(IServiceScope scope, PennyDbContext db, AuctionItem initial, CancellationToken ct) {
		var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();
		// 次回入札タイミングを決定して待機
		var slackSec = Random.Shared.Next(5, 20);
		var targetAt = initial.EndTime.AddSeconds(-slackSec);
		var now = DateTime.UtcNow;
		if (targetAt > now) {
			var delay = targetAt - now;
			await Task.Delay(delay, ct);
		}

		// Botユーザー選定（現最高入札者は除外）
		var botUserId = BotUserCache.GetRandomBotUserId(initial.CurrentHighestBidUserId);
		if (botUserId is null) {
			this._logger.LogWarning("No available bot users for Item {ItemId}", this._auctionItemId);
			return;
		}

		try {
			await auctionService.PlaceBidAsync(botUserId.Value, new() { AuctionItemId = this._auctionItemId, BidAmount = initial.CurrentPrice + initial.BidIncrement });
		} catch (ValidationPennyException ex) {
			this._logger.LogDebug(ex, "Validation during autobid for Item {ItemId}", this._auctionItemId);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Unexpected error during autobid for Item {ItemId}", this._auctionItemId);
		}
	}

	/// <summary>
	///     終了処理
	/// </summary>
	/// <param name="scope">Scope</param>
	/// <param name="db">DbContext</param>
	/// <param name="initial">初回取得値</param>
	/// <param name="ct">CancellationToken</param>
	private async Task FinalizeAsync(IServiceScope scope, PennyDbContext db, AuctionItem initial, CancellationToken ct) {
		var hub = scope.ServiceProvider.GetRequiredService<IHubContext<AuctionHub, IAuctionClient>>();
		var now = DateTime.UtcNow;

		var remaining = initial.EndTime - now;
		if (remaining > TimeSpan.Zero) {
			try {
				await Task.Delay(remaining, ct);
			} catch (OperationCanceledException) {
				return;
			}
		}

		await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

		var latest = await this.GetLatestAuctionItemAsync(db, initial, ct);
		if (latest is null) {
			return;
		}

		// End → ステータス更新
		latest.Status = AuctionStatus.Ended;
		await db.SaveChangesAsync(ct);
		await tx.CommitAsync(ct);

		var dto = new AuctionClosedDto {
			AuctionItemId = latest.Id,
			FinalPrice = latest.CurrentPrice,
			EndTime = latest.EndTime,
			Status = latest.Status,
			WinnerUserId = (int)latest.CurrentHighestBidUserId!
		};

		await hub.Clients.Group(AuctionHub.BuildGroupName(latest.Id)).ReceiveAuctionClosed(dto);
		this._logger.LogInformation("Auction {ItemId} closed and notified.", latest.Id);
	}

	private async Task<AuctionItem?> GetLatestAuctionItemAsync(PennyDbContext db, AuctionItem initial, CancellationToken ct) {
		var latest = await db.AuctionItems
			.Where(x => x.Id == this._auctionItemId)
			.FirstOrDefaultAsync(ct);

		if (latest is null) {
			this._logger.LogWarning("Item {ItemId} no longer exists.", this._auctionItemId);
			return null;
		}

		// 状態が変わっていたら（時間延長されていたら）今回は終了し、Coordinatorに任せる
		if (latest.EndTime != initial.EndTime) {
			this._logger.LogDebug("Item {ItemId} state changed before bid. EndTime/Price updated.", this._auctionItemId);
			return null;
		}

		return latest;
	}
}