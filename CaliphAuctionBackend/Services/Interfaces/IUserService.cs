using CaliphAuctionBackend.Dtos.User;

namespace CaliphAuctionBackend.Services.Interfaces;

public interface IUserService {
	public Task RegisterAsync(RegisterUserDto registerUserDto);
	public Task<LoginResultDto> LoginAsync(LoginDto loginDto, string ipAddress);
	public Task<UserSummaryDto?> GetByIdAsync(int userId);
}