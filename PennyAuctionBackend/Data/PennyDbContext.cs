using Microsoft.EntityFrameworkCore;
using PennyAuctionBackend.Models;

namespace PennyAuctionBackend.Data;

public class PennyDbContext(DbContextOptions<PennyDbContext> options) : DbContext(options) {
	public required DbSet<User> Users {
		get;
		set;
	}

	public required DbSet<FailedLoginAttempt> FailedLoginAttempts {
		get;
		set;
	}

	public override int SaveChanges() {
		this.UpdateTimestamps();
		return base.SaveChanges();
	}

	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
		this.UpdateTimestamps();
		return base.SaveChangesAsync(cancellationToken);
	}

	private void UpdateTimestamps() {
		var entries = this.ChangeTracker.Entries<BaseEntity>();
		var now = DateTime.UtcNow;

		foreach (var entry in entries) {
			switch (entry.State) {
				case EntityState.Added:
					entry.Entity.CreatedAt = now;
					entry.Entity.UpdatedAt = now;
					break;
				case EntityState.Modified:
					entry.Entity.UpdatedAt = now;
					break;
			}
		}
	}
}