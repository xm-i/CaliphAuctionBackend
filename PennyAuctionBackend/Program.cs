namespace PennyAuctionBackend;

public static class Program {
	public static void Main(string[] args) {
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddControllers();
		builder.Services.AddSignalR();
		builder.Services.AddOpenApi();

		builder.Services.AddAuthorization(options => {
			options.FallbackPolicy = options.DefaultPolicy;
		});

		var app = builder.Build();

		if (app.Environment.IsDevelopment()) {
			app.MapOpenApi();
		}

		app.UseHttpsRedirection();

		app.MapControllers();
		app.Run();
	}
}