using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CaliphAuctionBackend.Data;
using CaliphAuctionBackend.Hubs;
using CaliphAuctionBackend.Services.Background;
using CaliphAuctionBackend.Services.Infrastructure;
using CaliphAuctionBackend.Utils.Attributes;

namespace CaliphAuctionBackend;

public static class Program {
	public static void Main(string[] args) {
		var app = Build(args);

		BotUserCache.Initialize(app.Services);

		if (app.Environment.IsDevelopment()) {
			app.MapOpenApi();
		}

		app.UseHttpsRedirection();
		app.UseCors("DefaultCorsPolicy");
		app.UseAuthentication();
		app.UseAuthorization();

		app.MapControllers();
		app.MapHub<AuctionHub>("/auctionHub");

		app.Run();
	}

	private static WebApplication Build(string[] args) {
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddControllers();
		builder.Services.AddSignalR();
		builder.Services.AddOpenApi();
		builder.Services.AddHttpContextAccessor();
		builder.Services.AddDbContext<CaliphDbContext>(options =>
			options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")!));

		RegisterServices(builder.Services);

		builder.Services.Configure<AuctionOptions>(builder.Configuration.GetSection("Auction"));
		builder.Services.Configure<AutoBidOptions>(builder.Configuration.GetSection("AutoBid"));

		builder.Services.AddHostedService<AuctionTopUpService>();
		builder.Services.AddHostedService<AutoBidCoordinatorService>();

		var jwtKey = builder.Configuration["Jwt:Key"];
		var jwtIssuer = builder.Configuration["Jwt:Issuer"];
		builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options => {
				options.TokenValidationParameters = new() {
					ValidateIssuer = true,
					ValidateAudience = false,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtIssuer,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
				};
			});
		var allowedOrigins = builder.Configuration
			.GetSection("Cors:AllowedOrigins")
			.Get<string[]>()!;

		builder.Services.AddCors(options => {
			options.AddPolicy("DefaultCorsPolicy", policy => {
				policy.WithOrigins(allowedOrigins)
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials();
			});
		});

		builder.Services.AddAuthorization(options => {
			options.FallbackPolicy = options.DefaultPolicy;
		});

		return builder.Build();
	}

	private static void RegisterServices(IServiceCollection serviceCollection) {
		var targetTypes = Assembly
			.GetExecutingAssembly()
			.GetTypes()
			.Where(x =>
				x.GetCustomAttributes<AddScopedAttribute>(true).Any());

		foreach (var targetType in targetTypes) {
			var attribute = targetType.GetCustomAttribute<AddScopedAttribute>();
			var serviceType = attribute?.ServiceType ?? targetType.GetInterfaces().FirstOrDefault(x => x.Name.ToString() == $"I{targetType.Name}", targetType);
			serviceCollection.AddScoped(serviceType, targetType);
		}
	}
}