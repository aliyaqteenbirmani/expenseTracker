using SpendwiseSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SpendwiseEntity = SpendwiseSystem.Domain.Entities.Spendwise;

namespace SpendwiseSystem.Infrastructure.Data.Migrations
{
    // Ensure the correct DbContext base class is referenced, not a namespace
    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<SpendwiseEntity> Spendwises { get; set; }
        public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}



