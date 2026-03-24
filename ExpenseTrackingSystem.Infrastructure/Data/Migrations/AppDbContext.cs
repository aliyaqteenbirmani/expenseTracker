using ExpenseTrackingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackingSystem.Infrastructure.Data.Migrations
{
    // Ensure the correct DbContext base class is referenced, not a namespace
    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
