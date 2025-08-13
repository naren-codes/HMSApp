using Microsoft.EntityFrameworkCore;
using HMSApp.Models;

namespace HMSApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        

        public DbSet<Patient> Patient { get; set; }
        public DbSet<Doctor> Doctor { get; set; }
        public DbSet<Appointment> Appointment { get; set; }
        public DbSet<Bill> Bill { get; set; }
        public DbSet<User> User { get; set; }

    }
}
