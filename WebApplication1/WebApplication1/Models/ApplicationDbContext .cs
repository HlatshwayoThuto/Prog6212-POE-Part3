using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace WebApplication1.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Claim> Claims { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // relationships
            modelBuilder.Entity<Claim>()
                .HasMany(c => c.Documents)
                .WithOne()
                .HasForeignKey(d => d.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            // configure decimal precision
            modelBuilder.Entity<Claim>()
                .Property(c => c.HoursWorked).HasColumnType("REAL");
            modelBuilder.Entity<Claim>()
                .Property(c => c.HourlyRate).HasColumnType("REAL");
            modelBuilder.Entity<User>()
                .Property(u => u.HourlyRate).HasColumnType("REAL");

            base.OnModelCreating(modelBuilder);
        }
    }
}