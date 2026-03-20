using CaliphAuctionBackend.Dtos.User;

namespace CaliphAuctionBackend.Services.Interfaces;

public interface IUserService {
	public Task<PreRegisterResultDto> PreRegisterAsync(PreRegisterUserDto preRegisterUserDto);
	public Task RegisterAsync(RegisterUserDto registerUserDto, int userId);
	public Task<LoginResultDto> LoginAsync(LoginDto loginDto, string ipAddress);
	public Task<UserSummaryDto?> GetByIdAsync(int userId);
}