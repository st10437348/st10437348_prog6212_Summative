using Microsoft.EntityFrameworkCore;
using CMCSSummative.Models;

namespace CMCSSummative.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

        public DbSet<UserAccount> Users { get; set; } = null!;
        public DbSet<Lecturer> Lecturers { get; set; } = null!;
        public DbSet<Claim> Claims { get; set; } = null!;
        public DbSet<Approval> Approvals { get; set; } = null!;
        public DbSet<SupportingDocument> Documents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<CMCSSummative.Models.UserAccount>(eb =>
            {
                eb.HasKey(u => u.UserId);
                eb.ToTable("Users");
                eb.HasIndex(u => u.Username).IsUnique();
            });

            mb.Entity<CMCSSummative.Models.Lecturer>(eb =>
            {
                eb.HasKey(l => l.LecturerId);
                eb.ToTable("Lecturers");
                eb.Property(l => l.Name).HasMaxLength(200);
                eb.HasOne<CMCSSummative.Models.UserAccount>()
                  .WithMany()
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            mb.Entity<CMCSSummative.Models.Claim>(eb =>
            {
                eb.HasKey(c => c.ClaimId);
                eb.ToTable("Claims");
                eb.HasOne<CMCSSummative.Models.Lecturer>()
                  .WithMany()
                  .HasForeignKey(c => c.LecturerId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            mb.Entity<CMCSSummative.Models.Approval>(eb =>
            {
                eb.HasKey(a => a.ApprovalId);
                eb.ToTable("Approvals");
                eb.HasOne<CMCSSummative.Models.Claim>()
                  .WithMany(c => c.Approvals)
                  .HasForeignKey(a => a.ClaimId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            mb.Entity<CMCSSummative.Models.SupportingDocument>(eb =>
            {
                eb.HasKey(d => d.DocumentId);
                eb.ToTable("SupportingDocuments");
                eb.Property(d => d.FileName).HasMaxLength(500);
                eb.Property(d => d.FilePath).HasMaxLength(2000);
                eb.HasOne<CMCSSummative.Models.Claim>()
                  .WithMany(c => c.Documents)
                  .HasForeignKey(d => d.ClaimId)
                  .OnDelete(DeleteBehavior.Cascade);
            });
        }

    }
}
