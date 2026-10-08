using Microsoft.EntityFrameworkCore;

namespace ServiceApresVenteApp.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Intervention> Interventions { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<SparePart> SpareParts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Configure warnings
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.Client)
                .WithMany(c => c.Complaints)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.Article)
                .WithMany()
                .HasForeignKey(c => c.ArticleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Intervention>()
                .HasOne(i => i.Complaint)
                .WithMany(c => c.Interventions)
                .HasForeignKey(i => i.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Article>()
                .HasOne(a => a.Client)
                .WithMany()
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); // ClientId is nullable

            modelBuilder.Entity<Article>()
                .Property(a => a.Description)
                .IsRequired(); // Make Description required

            modelBuilder.Entity<SparePart>()
                .HasOne(sp => sp.Intervention)
                .WithMany(i => i.SpareParts)
                .HasForeignKey(sp => sp.InterventionId)
                .OnDelete(DeleteBehavior.Cascade); // Change to Cascade to automatically delete spare parts

            modelBuilder.Entity<Client>().HasData(
                new Client
                {
                    Id = 1,
                    Name = "Admin User",
                    Email = "admin@example.com",
                    Password = "Admin123!",
                    Role = "Admin"
                }
            );
        }
    }
}
