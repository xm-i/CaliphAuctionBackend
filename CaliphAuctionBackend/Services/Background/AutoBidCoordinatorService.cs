using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CaliphAuctionBackend.Data;
using CaliphAuctionBackend.Models;

namespace CaliphAuctionBackend.Services.Background;

public class AutoBidOptions {
	public int DiscoveryIntervalSeconds {
		get;
		set;
	}
}

public class AutoBidCoordinatorService(
	IServiceScopeFactory scopeFactory,
	ILogger<AutoBidCoordinatorService> logger,
	IOptions<AutoBidOptions> options) : BackgroundService {
	private readonly ILogger<AutoBidCoordinatorService> _logger = logger;
	private readonly AutoBidOptions _options = options.Value;
	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

	private readonly Dictionary<int, (CancellationTokenSource Cts, Task Task)> _workers = new();

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		this._logger.LogInformation("AutoBidCoordinator started.");

		while (!stoppingToken.IsCancellationRequested) {
			try {
				await this.DiscoverAndSyncWorkersAsync(stoppingToken);
			} catch (OperationCanceledException) {
				break;
			} catch (Exception ex) {
				this._logger.LogError(ex, "Coordinator loop error");
			}

			await Task.Delay(TimeSpan.FromSeconds(this._options.DiscoveryIntervalSeconds), stoppingToken);
		}

		// 停止時に全ワーカーをキャンセル
		foreach (var (_, entry) in this._workers.ToArray()) {
			try {
				await entry.Cts.CancelAsync();
			} catch {
				// ignore
			}
		}

		await Task.WhenAll(this._workers.Values.Select(v => v.Task).ToArray());
		this._logger.LogInformation("AutoBidCoordinator stopped.");
	}

	private async Task DiscoverAndSyncWorkersAsync(CancellationToken ct) {
		using var scope = this._scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<CaliphDbContext>();
		var targetTime = DateTime.UtcNow.AddSeconds(this._options.DiscoveryIntervalSeconds * 2);

		var candidates = await db.AuctionItems
			.AsNoTracking()
			.Where(x =>
				x.Status == AuctionStatus.Active &&
				x.EndTime < targetTime)
			.Select(x => x.Id)
			.ToListAsync(ct);

		// 完了済みワーカーを掃除
		foreach (var kv in this._workers.ToArray()) {
			if (kv.Value.Task.IsCompleted) {
				this._workers.Remove(kv.Key);
			}
		}

		// 新規開始
		foreach (var itemId in candidates) {
			if (this._workers.ContainsKey(itemId)) {
				continue;
			}

			var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			var workerLogger = scope.ServiceProvider.GetRequiredService<ILogger<AutoBidWorker>>();
			var worker = new AutoBidWorker(itemId, this._scopeFactory, workerLogger);
			var task = Task.Run(() => worker.RunAsync(linkedCts.Token), linkedCts.Token);
			this._workers[itemId] = (linkedCts, task);

			this._logger.LogInformation("AutoBidWorker launched for Item {ItemId}", itemId);
		}

		// 終了すべきワーカー（候補外）を停止
		var toStop = this._workers.Keys.Except(candidates).ToList();
		foreach (var itemId in toStop) {
			if (!this._workers.Remove(itemId, out var entry)) {
				continue;
			}

			try {
				await entry.Cts.CancelAsync();
			} catch {
				// ignore
			}
		}
	}
}