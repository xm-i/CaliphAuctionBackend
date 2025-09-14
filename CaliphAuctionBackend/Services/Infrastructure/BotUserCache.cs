using CaliphAuctionBackend.Data;

namespace CaliphAuctionBackend.Services.Infrastructure;

public static class BotUserCache {
	private static int[] _ids = [];
	private static volatile bool _initialized;
	private static readonly Lock Lock = new();

	public static IReadOnlyList<int> AllBotUserIds {
		get {
			return _ids;
		}
	}

	public static void Initialize(IServiceProvider services) {
		if (_initialized) {
			return;
		}

		lock (Lock) {
			if (_initialized) {
				return;
			}

			using var scope = services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<CaliphDbContext>();
			_ids = db.Users
				.Where(u => u.IsBotUser)
				.Select(u => u.Id)
				.ToArray();
			_initialized = true;
		}
	}

	public static int? GetRandomBotUserId(int? excludeUserId = null) {
		var local = _ids;
		if (local.Length == 0) {
			return null;
		}

		if (!excludeUserId.HasValue) {
			return local[Random.Shared.Next(local.Length)];
		}

		var filtered = local.Where(id => id != excludeUserId.Value).ToArray();
		if (filtered.Length == 0) {
			return null;
		}

		return filtered[Random.Shared.Next(filtered.Length)];
	}
}