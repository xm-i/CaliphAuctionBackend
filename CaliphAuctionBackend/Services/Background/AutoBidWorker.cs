using System.Data;
using CaliphAuctionBackend.Data;
using CaliphAuctionBackend.Dtos.Realtime;
using CaliphAuctionBackend.Exceptions;
using CaliphAuctionBackend.Hubs;
using CaliphAuctionBackend.Models;
using CaliphAuctionBackend.Services.Infrastructure;
using CaliphAuctionBackend.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CaliphAuctionBackend.Services.Background;

public class AutoBidWorker(
	int auctionItemId,
	IServiceScopeFactory scopeFactory,
	ILogger<AutoBidWorker> logger) {
	private readonly int _auctionItemId = auctionItemId;
	private readonly ILogger<AutoBidWorker> _logger = logger;
	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

	public async Task RunAsync(CancellationToken ct) {
		this._logger.LogDebug("AutoBidWorker started for Item {ItemId}", this._auctionItemId);

		try {
			using var scope = this._scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<CaliphDbContext>();

			// 最新状態取得
			var initial = await db.AuctionItems
				.Include(x => x.CurrentHighestBidUser)
				.ThenInclude(x => x!.PointPurchases)
				.AsNoTracking()
				.Where(x => x.Id == this._auctionItemId)
				.FirstOrDefaultAsync(ct);

			if (initial is null) {
				this._logger.LogDebug("Item {ItemId} no longer exists. Stop worker.", this._auctionItemId);
				return;
			}

			if (initial.Status != AuctionStatus.Active) {
				this._logger.LogDebug("Item {ItemId} not active. Stop worker.", this._auctionItemId);
				return;
			}

			//　価格+入札済みコストが最低価格に達していて、且つ過去にポイント購入したことのあるユーザーの場合は終了処理へ
			if (initial.CurrentPrice + initial.TotalBidCost >= initial.MinimumPrice &&
			    (initial.CurrentHighestBidUser?.PointPurchases.Count ?? 0) > 0) {
				await this.FinalizeAsync(scope, db, initial, ct);
				return;
			}

			// 最低価格に到達していなくてもBOTが最高入札者なら一定確率で終了
			if (initial.CurrentHighestBidUser?.IsBotUser == true && new Random().NextDouble() < 0.08) {
				await this.FinalizeAsync(scope, db, initial, ct);
				return;
			}

			await this.BotBidAsync(scope, initial, ct);
		} catch (OperationCanceledException) {
			// 正常停止
		} catch (Exception ex) {
			this._logger.LogError(ex, "AutoBidWorker error for Item {ItemId}", this._auctionItemId);
		} finally {
			this._logger.LogDebug("AutoBidWorker stopped for Item {ItemId}", this._auctionItemId);
		}
	}

	/// <summary>
	///     BOT即時入札
	/// </summary>
	/// <param name="ct"></param>
	public async Task BotBidImmediatelyAsync(CancellationToken ct) {
		this._logger.LogDebug("Immediate autobid requested for Item {ItemId}", this._auctionItemId);
		try {
			using var scope = this._scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<CaliphDbContext>();
			var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();

			var item = await db.AuctionItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == this._auctionItemId, ct);
			if (item is null) {
				this._logger.LogWarning("Immediate autobid: item {ItemId} not found", this._auctionItemId);
				return;
			}

			if (item.Status != AuctionStatus.Active) {
				this._logger.LogDebug("Immediate autobid: item {ItemId} not active", this._auctionItemId);
				return;
			}

			var botUserId = BotUserCache.GetRandomBotUserId(item.CurrentHighestBidUserId);
			if (botUserId is null) {
				this._logger.LogWarning("Immediate autobid: no bot user available for {ItemId}", this._auctionItemId);
				return;
			}

			try {
				await auctionService.PlaceBidAsync(botUserId.Value, new() { AuctionItemId = this._auctionItemId, BidAmount = item.CurrentPrice + item.BidIncrement }, "AUTO_BOT_IMMEDIATE");
				this._logger.LogDebug("Immediate autobid placed by BotUser {UserId} on Item {ItemId}", botUserId.Value, this._auctionItemId);
			} catch (ValidationCaliphException ex) {
				this._logger.LogDebug(ex, "Immediate autobid validation issue for Item {ItemId}", this._auctionItemId);
			} catch (Exception ex) {
				this._logger.LogError(ex, "Immediate autobid unexpected error for Item {ItemId}", this._auctionItemId);
			}
		} catch (OperationCanceledException) {
			// ignore
		}
	}

	/// <summary>
	///     Bot入札処理
	/// </summary>
	/// <param name="scope">Scope</param>
	/// <param name="initial">初回取得値</param>
	/// <param name="ct">CancellationToken</param>
	private async Task BotBidAsync(IServiceScope scope, AuctionItem initial, CancellationToken ct) {
		var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();
		// 次回入札タイミングを決定して待機
		var slackSec = Random.Shared.Next(2, 30);
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
			await auctionService.PlaceBidAsync(botUserId.Value, new() { AuctionItemId = this._auctionItemId, BidAmount = initial.CurrentPrice + initial.BidIncrement }, "AUTO_BOT");
		} catch (ValidationCaliphException ex) {
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
	private async Task FinalizeAsync(IServiceScope scope, CaliphDbContext db, AuctionItem initial, CancellationToken ct) {
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

		// 落札ユーザー通知 (ユーザーが存在する場合のみ)
		if (latest.CurrentHighestBidUserId.HasValue) {
			db.Notifications.Add(new() {
				UserId = latest.CurrentHighestBidUserId.Value,
				Category = "bidWin",
				Title = "落札おめでとうございます",
				Message = $"『{latest.Name}』を {latest.CurrentPrice} で落札しました。",
				IsRead = false
			});
		}

		await db.SaveChangesAsync(ct);
		await tx.CommitAsync(ct);

		var dto = new AuctionClosedDto {
			AuctionItemId = latest.Id,
			FinalPrice = latest.CurrentPrice,
			EndTime = latest.EndTime,
			Status = latest.Status,
			WinnerUserId = (int)latest.CurrentHighestBidUserId!
		};

		await hub.Clients.All.ReceiveAuctionClosed(dto);
		this._logger.LogInformation("Auction {ItemId} closed, notification created and clients notified.", latest.Id);
	}

	private async Task<AuctionItem?> GetLatestAuctionItemAsync(CaliphDbContext db, AuctionItem initial, CancellationToken ct) {
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