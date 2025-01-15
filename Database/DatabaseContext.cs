using Microsoft.EntityFrameworkCore;
using mPass_server.Database.Models;

namespace mPass_server.Database;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<ServicePassword> Passwords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServicePassword>()
            .HasOne(p => p.User)
            .WithMany(u => u.Passwords)
            .HasForeignKey(p => p.UserId);

        base.OnModelCreating(modelBuilder);
    }
}