namespace CaliphAuctionBackend.Dtos.User;

public class PreRegisterResultDto {
    public int UserId {
        get;
        set;
    }

    public required string Password {
        get;
        set;
    }
}
