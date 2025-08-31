using PennyAuctionBackend.Dtos.User;

namespace PennyAuctionBackend.Services.Interfaces;

public interface IUserService {
	public Task RegisterAsync(RegisterUserDto registerUserDto);
	public Task<LoginResultDto> LoginAsync(LoginDto loginDto, string ipAddress);
}