using HMSApp.Data;
using HMSApp.Models;
using Microsoft.EntityFrameworkCore;

namespace HMSApp.Services
{
    public class DoctorService
    {
        private readonly ApplicationDbContext _context;

        public DoctorService(ApplicationDbContext context)
        {
            _context = context;
        }
        public List<Doctor> GetAllDoctors() =>
            _context.Doctor.ToList();
    }
}
