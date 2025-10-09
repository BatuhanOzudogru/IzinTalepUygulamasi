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
    }
}
