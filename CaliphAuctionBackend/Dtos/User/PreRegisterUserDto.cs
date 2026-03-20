using System.ComponentModel.DataAnnotations;

namespace CaliphAuctionBackend.Dtos.User;

public class PreRegisterUserDto {
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3-50 characters.")]
    public required string Username {
        get;
        set;
    }
}
