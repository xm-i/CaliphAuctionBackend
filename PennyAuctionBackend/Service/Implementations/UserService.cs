using Microsoft.EntityFrameworkCore;
using PennyAuctionBackend.Data;
using PennyAuctionBackend.Dtos.User;
using PennyAuctionBackend.Exceptions;
using PennyAuctionBackend.Models;
using PennyAuctionBackend.Service.Interfaces;
using PennyAuctionBackend.Utils;
using PennyAuctionBackend.Utils.Attributes;

namespace PennyAuctionBackend.Service.Implementations;

[AddScoped]
public class UserService(PennyDbContext dbContext, IConfiguration configuration) : IUserService {
	private readonly IConfiguration _configuration = configuration;
	private readonly PennyDbContext _dbContext = dbContext;

	/// <summary>
	///     新しいユーザーを登録する
	/// </summary>
	/// <param name="registerUserDto">登録するユーザー情報が入った DTO</param>
	/// <returns>処理完了の Task</returns>
	public async Task RegisterAsync(RegisterUserDto registerUserDto) {
		await using var transaction = await this._dbContext.Database.BeginTransactionAsync();

		await this.ValidateAsync(registerUserDto);

		var salt = SecurityUtils.GenerateSalt();
		var hashedPassword = SecurityUtils.HashPassword(registerUserDto.Password, salt);

		var user = new User {
			Email = registerUserDto.Email,
			PasswordSalt = salt,
			PasswordHash = hashedPassword,
			Username = registerUserDto.Username,
			CreatedAt = DateTime.UtcNow,
			EmailConfirmed = false,
			LastFailedLoginAt = null,
			FailedLoginCount = 0,
			IsDeleted = false
		};

		this._dbContext.Users.Add(user);
		await this._dbContext.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	/// <summary>
	///     指定されたメールアドレスとパスワードでログインし、認証に成功した場合、JWTトークンとユーザー情報を返す
	/// </summary>
	/// <param name="loginDto">ログイン情報を含むDTO</param>
	/// <param name="ipAddress">ログイン試行を行っているクライアントのIPアドレス</param>
	/// <returns>JWTトークンとユーザー情報</returns>
	public async Task<LoginResultDto> LoginAsync(LoginDto loginDto, string ipAddress) {
		await using var transaction = await this._dbContext.Database.BeginTransactionAsync();
		var oneHourAgo = DateTime.UtcNow.AddHours(-1);
		var failCount = await this._dbContext.FailedLoginAttempts
			.CountAsync(f => f.IpAddress == ipAddress && f.AttemptedAt > oneHourAgo);
		if (failCount >= 10) {
			throw new IpBlockedPennyException("Too many failed login attempts from this IP.");
		}

		var user = await this._dbContext.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email && !u.IsDeleted);
		if (user is null) {
			throw new AuthenticationFailedPennyException("Invalid email or password.");
		}

		if (user.LastFailedLoginAt is not null && user.LastFailedLoginAt < oneHourAgo) {
			user.FailedLoginCount = 0;
		}

		if (user is { FailedLoginCount: >= 10, LastFailedLoginAt: not null } && user.LastFailedLoginAt > oneHourAgo) {
			throw new AccountLockedPennyException("Your account is temporarily locked. Please try again later.");
		}

		try {
			var hashedInput = SecurityUtils.HashPassword(loginDto.Password, user.PasswordSalt);

			if (hashedInput != user.PasswordHash) {
				throw new AuthenticationFailedPennyException("Invalid email or password.");
			}

			var jwtKey = this._configuration["Jwt:Key"];
			var jwtIssuer = this._configuration["Jwt:Issuer"];
			if (string.IsNullOrEmpty(jwtKey) ||
			    string.IsNullOrEmpty(jwtIssuer) ||
			    !int.TryParse(this._configuration["Jwt:ExpireMinutes"], out var expireMinutes)) {
				throw new ConfigurationPennyException("JWT configuration is missing.");
			}

			var token = SecurityUtils.GenerateJwtToken(
				user.Id.ToString(),
				user.Email,
				jwtKey,
				jwtIssuer,
				expireMinutes
			);
			user.LastLoginAt = DateTime.UtcNow;
			user.FailedLoginCount = 0;

			var result = new LoginResultDto {
				AccessToken = token,
				User = new UserSummaryDto {
					Id = user.Id,
					Email = user.Email,
					Username = user.Username
				}
			};
			return result;
		} catch (PennyException) {
			user.FailedLoginCount++;
			user.LastFailedLoginAt = DateTime.UtcNow;
			this._dbContext.FailedLoginAttempts.Add(new() { Email = loginDto.Email, IpAddress = ipAddress, AttemptedAt = DateTime.UtcNow });
			throw;
		} finally {
			await this._dbContext.SaveChangesAsync();
			await transaction.CommitAsync();
		}
	}

	private async Task ValidateAsync(RegisterUserDto request) {
		if (await this._dbContext.Users.AnyAsync(u => u.Email == request.Email)) {
			throw new ValidationPennyException("Email already exists.");
		}

		if (await this._dbContext.Users.AnyAsync(u => u.Username == request.Username)) {
			throw new ValidationPennyException("Username already exists.");
		}
	}
}