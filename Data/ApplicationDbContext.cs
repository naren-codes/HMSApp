using Microsoft.EntityFrameworkCore;
using HMSApp.Models;

namespace HMSApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patient { get; set; }
        public DbSet<User> User { get; set; }

    }
}
