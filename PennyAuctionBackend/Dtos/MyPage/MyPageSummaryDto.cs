namespace PennyAuctionBackend.Dtos.MyPage;

public class MyPageSummaryDto {
	public int PointBalance {
		get;
		set;
	}

	public int TotalSpentAmount {
		get;
		set;
	}

	public IReadOnlyCollection<NotificationDto> Notifications {
		get;
		set;
	} = [];
}

public class NotificationDto {
	public long Id {
		get;
		set;
	}

	public string? Category {
		get;
		set;
	}

	public required string Title {
		get;
		set;
	}

	public required string Message {
		get;
		set;
	}

	public DateTime CreatedAt {
		get;
		set;
	}

	public bool IsRead {
		get;
		set;
	}
}