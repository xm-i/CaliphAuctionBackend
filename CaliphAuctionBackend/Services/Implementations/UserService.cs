using System.Security.Cryptography;
using CaliphAuctionBackend.Data;
using CaliphAuctionBackend.Dtos.User;
using CaliphAuctionBackend.Exceptions;
using CaliphAuctionBackend.Models;
using CaliphAuctionBackend.Services.Interfaces;
using CaliphAuctionBackend.Utils;
using CaliphAuctionBackend.Utils.Attributes;
using Microsoft.EntityFrameworkCore;

namespace CaliphAuctionBackend.Services.Implementations {
	[AddScoped]
	public class UserService(CaliphDbContext dbContext, IConfiguration configuration) : IUserService {
		private readonly IConfiguration _configuration = configuration;
		private readonly CaliphDbContext _dbContext = dbContext;

		private string Pepper {
			get {
				return this._configuration["Security:PasswordPepper"] ?? throw new ConfigurationCaliphException("Pepper configuration is missing.");
			}
		}

		/// <summary>
		///     ユーザー名のみで仮登録を行い、ログイン用の仮パスワードを返す
		/// </summary>
		public async Task<PreRegisterResultDto> PreRegisterAsync(PreRegisterUserDto preRegisterUserDto) {
			await using var transaction = await this._dbContext.Database.BeginTransactionAsync();

			await this.ValidateUsernameAsync(preRegisterUserDto.Username);

			var temporaryPassword = this.GenerateTemporaryPassword();
			var salt = SecurityUtils.GenerateSalt();
			var hashedPassword = SecurityUtils.HashPassword(temporaryPassword, salt, this.Pepper);
			var nullableRegistrationBonusPoints = this._configuration.GetValue<int?>("Points:RegistrationBonus");
			if (nullableRegistrationBonusPoints is not { } registrationBonusPoints) {
				throw new ConfigurationCaliphException("Registration bonus points configuration is missing.");
			}

			var user = new User {
				Email = this.GenerateTemporaryEmail(),
				PasswordSalt = salt,
				PasswordHash = hashedPassword,
				Username = preRegisterUserDto.Username,
				CreatedAt = DateTime.UtcNow,
				EmailConfirmed = false,
				LastFailedLoginAt = null,
				FailedLoginCount = 0,
				IsDeleted = false,
				IsBotUser = false,
				IsPreRegistered = true,
				PointBalance = registrationBonusPoints
			};

			this._dbContext.Users.Add(user);
			await this._dbContext.SaveChangesAsync();

			// 会員登録ボーナス付与 (キャンペーンID=0 固定)
			if (registrationBonusPoints > 0) {
				var lot = new PointBalanceLot { UserId = user.Id, UnitPrice = 0m, QuantityRemaining = registrationBonusPoints };
				this._dbContext.PointBalanceLots.Add(lot);

				var pointTransaction = new PointTransaction {
					UserId = user.Id,
					Type = PointTransactionType.BonusGrant,
					TotalAmount = registrationBonusPoints,
					BalanceAfter = user.PointBalance,
					CampaignId = 0,
					Note = "Registration bonus"
				};
				this._dbContext.PointTransactions.Add(pointTransaction);

				var entry = new PointTransactionEntry {
					PointTransactionId = pointTransaction.Id,
					Quantity = registrationBonusPoints,
					PointBalanceLotId = lot.Id,
					UnitPrice = 0m,
					TotalPrice = 0
				};
				pointTransaction.Entries.Add(entry);
				lot.PointTransactionEntries.Add(entry);
				this._dbContext.PointTransactionEntries.Add(entry);
			}

			await this._dbContext.SaveChangesAsync();
			await transaction.CommitAsync();

			return new PreRegisterResultDto {
				UserId = user.Id,
				Password = temporaryPassword
			};
		}

		/// <summary>
		///     仮登録済みユーザーの情報を更新して本登録を完了する（メールアドレスとパスワードのみ更新）
		/// </summary>
		/// <param name="registerUserDto">更新するユーザー情報が入った DTO</param>
		/// <param name="userId">JWTトークンから取得したユーザーID</param>
		/// <returns>処理完了の Task</returns>
		public async Task RegisterAsync(RegisterUserDto registerUserDto, int userId) {
			await using var transaction = await this._dbContext.Database.BeginTransactionAsync();

			var user = await this._dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
			if (user is null) {
				throw new ValidationCaliphException("User not found.");
			}

			if (!user.IsPreRegistered) {
				throw new ValidationCaliphException("User is already registered.");
			}

			await this.ValidateEmailAsync(registerUserDto.Email, user.Id);

			var salt = SecurityUtils.GenerateSalt();
			var hashedPassword = SecurityUtils.HashPassword(registerUserDto.Password, salt, this.Pepper);

			user.Email = registerUserDto.Email;
			user.PasswordSalt = salt;
			user.PasswordHash = hashedPassword;
			user.IsPreRegistered = false;

			await this._dbContext.SaveChangesAsync();
			await transaction.CommitAsync();
		}

		/// <summary>
		///     指定されたメールアドレス or ユーザーID とパスワードでログインし、認証に成功した場合、JWTトークンとユーザー情報を返す
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
				throw new IpBlockedCaliphException("Too many failed login attempts from this IP.");
			}

			User? user;
			if (loginDto.UserId.HasValue) {
				user = await this._dbContext.Users.FirstOrDefaultAsync(u => u.Id == loginDto.UserId.Value && !u.IsDeleted);
			} else {
				var email = loginDto.Email ?? string.Empty;
				user = await this._dbContext.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
			}

			if (user is null) {
				throw new AuthenticationFailedCaliphException("Invalid userId/email or password.");
			}

			if (user.LastFailedLoginAt is not null && user.LastFailedLoginAt < oneHourAgo) {
				user.FailedLoginCount = 0;
			}

			if (user is { FailedLoginCount: >= 10, LastFailedLoginAt: not null } && user.LastFailedLoginAt > oneHourAgo) {
				throw new AccountLockedCaliphException("Your account is temporarily locked. Please try again later.");
			}

			try {
				var hashedInput = SecurityUtils.HashPassword(loginDto.Password, user.PasswordSalt, this.Pepper);

				if (hashedInput != user.PasswordHash) {
					throw new AuthenticationFailedCaliphException("Invalid userId/email or password.");
				}

				var jwtKey = this._configuration["Jwt:Key"];
				var jwtIssuer = this._configuration["Jwt:Issuer"];
				if (string.IsNullOrEmpty(jwtKey) ||
				    string.IsNullOrEmpty(jwtIssuer) ||
				    !int.TryParse(this._configuration["Jwt:ExpireMinutes"], out var expireMinutes)) {
					throw new ConfigurationCaliphException("JWT configuration is missing.");
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

				var result = new LoginResultDto { AccessToken = token, User = new() { Id = user.Id, Email = user.Email, Username = user.Username, IsPreRegistered = user.IsPreRegistered } };
				return result;
			} catch (CaliphException) {
				user.FailedLoginCount++;
				user.LastFailedLoginAt = DateTime.UtcNow;
				var attemptedIdentifier = !string.IsNullOrWhiteSpace(loginDto.Email)
					? loginDto.Email
					: $"user:{loginDto.UserId}";
				this._dbContext.FailedLoginAttempts.Add(new() { Email = attemptedIdentifier!, IpAddress = ipAddress, AttemptedAt = DateTime.UtcNow });
				throw;
			} finally {
				await this._dbContext.SaveChangesAsync();
				await transaction.CommitAsync();
			}
		}

		public async Task<UserSummaryDto?> GetByIdAsync(int userId) {
			var user = await this._dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
			return user == null ? null : new UserSummaryDto { Id = user.Id, Email = user.Email, Username = user.Username, IsPreRegistered = user.IsPreRegistered };
		}

		private async Task ValidateUsernameAsync(string username) {
			if (await this._dbContext.Users.AnyAsync(u => u.Username == username)) {
				throw new ValidationCaliphException("Username already exists.");
			}
		}

		private async Task ValidateEmailAsync(string email, int currentUserId) {
			if (await this._dbContext.Users.AnyAsync(u => u.Id != currentUserId && u.Email == email)) {
				throw new ValidationCaliphException("Email already exists.");
			}
		}

		private string GenerateTemporaryEmail() {
			return $"{Guid.NewGuid():N}@temp.local";
		}

		private string GenerateTemporaryPassword(int length = 12) {
			const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
			var buffer = new char[length];
			for (var i = 0; i < length; i++) {
				buffer[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
			}
			return new string(buffer);
		}
	}
}