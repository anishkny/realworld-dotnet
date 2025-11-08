using Microsoft.EntityFrameworkCore;

public class Db(DbContextOptions<Db> options) : DbContext(options)
{
  public DbSet<User> Users => Set<User>();
  public DbSet<Follow> Follows => Set<Follow>();
  public DbSet<Article> Articles => Set<Article>();

  // Set UpdatedAt before saving changes
  public override int SaveChanges()
  {
    UpdateTimestamps();
    return base.SaveChanges();
  }

  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    UpdateTimestamps();
    return await base.SaveChangesAsync(cancellationToken);
  }


  private void UpdateTimestamps()
  {
    var entries = ChangeTracker
      .Entries<BaseEntity>()
      .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

    foreach (var entry in entries)
    {
      entry.Entity.UpdatedAt = DateTime.UtcNow;

      if (entry.State == EntityState.Added)
        entry.Entity.CreatedAt = DateTime.UtcNow;
    }
  }
}
