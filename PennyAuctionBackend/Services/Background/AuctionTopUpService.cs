using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PennyAuctionBackend.Data;
using PennyAuctionBackend.Models;

namespace PennyAuctionBackend.Services.Background;

public class AuctionOptions {
	public int ActiveTargetCount {
		get;
		set;
	} = 50;

	public int TopUpIntervalSeconds {
		get;
		set;
	} = 30;
}

public class AuctionTopUpService(
	IServiceScopeFactory scopeFactory,
	IOptions<AuctionOptions> options,
	ILogger<AuctionTopUpService> logger) : BackgroundService {
	private readonly ILogger<AuctionTopUpService> _logger = logger;
	private readonly AuctionOptions _options = options.Value;
	private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		while (!stoppingToken.IsCancellationRequested) {
			try {
				await this.TopUpAsync(stoppingToken);
			} catch (Exception ex) {
				this._logger.LogError(ex, "TopUp failed");
			}

			await Task.Delay(TimeSpan.FromSeconds(this._options.TopUpIntervalSeconds), stoppingToken);
		}
	}

	private async Task TopUpAsync(CancellationToken ct) {
		using var scope = this._scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PennyDbContext>();

		var activeCount = await db.AuctionItems.CountAsync(x => x.Status == AuctionStatus.Active, ct);
		var shortage = Math.Max(0, this._options.ActiveTargetCount - activeCount);
		if (shortage == 0) {
			return;
		}

		var candidates = await db.Products
			.Where(p => p.IsActive && p.StockQuantity > 0)
			.Include(p => p.Category)
			.OrderBy(p => EF.Functions.Random())
			.Take(shortage)
			.ToListAsync(ct);

		if (candidates.Count == 0) {
			return;
		}

		foreach (var product in candidates) {
			var item = new AuctionItem {
				ProductId = product.Id,
				Name = product.Name,
				Description = product.Description,
				ThumbnailImageUrl = product.ThumbnailImageUrl,
				ImageUrl = product.ImageUrl,
				OriginalPrice = product.OriginalPrice,
				StartingBid = product.StartingBid,
				MinimumPrice = product.MinimumPrice,
				BidIncrement = product.BidIncrement,
				CurrentPrice = product.StartingBid,
				CategoryId = product.CategoryId,
				Category = product.Category,
				EndTime = DateTime.UtcNow.AddMinutes(product.DurationMinutes),
				Status = AuctionStatus.Active
			};
			db.AuctionItems.Add(item);

			product.StockQuantity -= 1;
		}

		await db.SaveChangesAsync(ct);
	}
}