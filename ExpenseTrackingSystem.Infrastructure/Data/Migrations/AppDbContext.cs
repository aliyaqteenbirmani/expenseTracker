using SpendwiseSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace SpendwiseSystem.Infrastructure.Data.Migrations
{
    // Ensure the correct DbContext base class is referenced, not a namespace
    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<CashBook> CashBooks { get; set; }
        public DbSet<CashTransaction> CashTransactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Business> Businesses { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<RefreshToken>()
                .Property(rt => rt.Id)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<RefreshToken>()
                .Property(rt => rt.Token)
                .HasMaxLength(200);

            modelBuilder.Entity<RefreshToken>()
                .Property(rt => rt.UserId)
                .HasMaxLength(50);
        }
    }
}



