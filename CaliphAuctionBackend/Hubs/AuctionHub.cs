using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using CaliphAuctionBackend.Dtos.Realtime;

namespace CaliphAuctionBackend.Hubs;

public interface IAuctionClient {
	public Task ReceiveBidUpdate(BidUpdateDto update);
	public Task ReceiveAuctionClosed(AuctionClosedDto closed);
}

[AllowAnonymous]
public class AuctionHub : Hub<IAuctionClient> {
	// 接続ごとに現在購読しているアイテムIDを管理
	private static readonly ConcurrentDictionary<string, HashSet<int>> VisibleItemsByConnection = new();

	public static string BuildGroupName(int auctionItemId) {
		return $"auction-item-{auctionItemId}";
	}

	public override Task OnDisconnectedAsync(Exception? exception) {
		VisibleItemsByConnection.TryRemove(this.Context.ConnectionId, out _);
		return base.OnDisconnectedAsync(exception);
	}

	public async Task SubscribeItem(int auctionItemId) {
		var connId = this.Context.ConnectionId;
		await this.Groups.AddToGroupAsync(connId, BuildGroupName(auctionItemId));
		var set = VisibleItemsByConnection.GetOrAdd(connId, _ => []);
		lock (set) {
			set.Add(auctionItemId);
		}
	}

	public async Task UnsubscribeItem(int auctionItemId) {
		var connId = this.Context.ConnectionId;
		await this.Groups.RemoveFromGroupAsync(connId, BuildGroupName(auctionItemId));
		if (VisibleItemsByConnection.TryGetValue(connId, out var set)) {
			lock (set) {
				set.Remove(auctionItemId);
			}
		}
	}

	/// <summary>
	///     検索一覧用：画面に可視なアイテムの完全集合を送ると差分で入退会する
	/// </summary>
	/// <param name="itemIds">itemIdリスト</param>
	public async Task SetVisibleItems(int[] itemIds) {
		var connId = this.Context.ConnectionId;
		var newSet = itemIds.ToHashSet();

		var current = VisibleItemsByConnection.GetOrAdd(connId, _ => []);
		HashSet<int> toAdd;
		HashSet<int> toRemove;
		lock (current) {
			toAdd = newSet.Except(current).ToHashSet();
			toRemove = current.Except(newSet).ToHashSet();
		}

		foreach (var id in toAdd) {
			await this.Groups.AddToGroupAsync(connId, BuildGroupName(id));
		}

		foreach (var id in toRemove) {
			await this.Groups.RemoveFromGroupAsync(connId, BuildGroupName(id));
		}

		lock (current) {
			current.Clear();
			foreach (var id in newSet) {
				current.Add(id);
			}
		}
	}
}