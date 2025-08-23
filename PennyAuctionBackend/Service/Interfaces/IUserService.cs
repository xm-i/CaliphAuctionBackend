using PennyAuctionBackend.Dtos.User;

namespace PennyAuctionBackend.Service.Interfaces;

public interface IUserService {
	public Task RegisterAsync(RegisterUserDto registerUserDto);
	public Task<LoginResultDto> LoginAsync(LoginDto loginDto, string ipAddress);
}