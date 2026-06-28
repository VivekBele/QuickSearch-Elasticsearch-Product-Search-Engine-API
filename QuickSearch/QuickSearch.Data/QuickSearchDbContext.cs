using Microsoft.EntityFrameworkCore;
using QuickSearch.Data.Entities;

namespace QuickSearch.Data
{
    public class QuickSearchDbContext : DbContext
    {
        public QuickSearchDbContext(DbContextOptions<QuickSearchDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserLog>()
                .HasKey(ul => new { ul.UserId, ul.LoginTime });

            base.OnModelCreating(modelBuilder);
        }
    }
}
