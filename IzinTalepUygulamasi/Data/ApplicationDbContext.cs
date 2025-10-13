using IzinTalepUygulamasi.Models;
using Microsoft.EntityFrameworkCore;

namespace IzinTalepUygulamasi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        public DbSet<ApprovalLog> ApprovalLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.RequestingEmployee)
                .WithMany()
                .HasForeignKey(lr => lr.RequestingEmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

        
            modelBuilder.Entity<ApprovalLog>()
                .HasOne(al => al.ProcessedByManager)
                .WithMany()
                .HasForeignKey(al => al.ProcessedByManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApprovalLog>()
                .HasOne(al => al.LeaveRequest)
                .WithMany()
                .HasForeignKey(al => al.LeaveRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
