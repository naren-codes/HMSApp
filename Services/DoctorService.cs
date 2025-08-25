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
        public List<Doctor> GetAllDoctors() => _context.Doctor.ToList();
        public Doctor? GetDoctor(int id) => _context.Doctor.FirstOrDefault(d => d.DoctorId == id);
        public Doctor? GetDoctorByUsername(string username) => _context.Doctor.FirstOrDefault(d => d.Username == username);
        public void AddDoctor(Doctor doctor)
        {
            _context.Doctor.Add(doctor);
            _context.SaveChanges();
        }
        public void UpdateDoctor(Doctor doctor)
        {
            _context.Doctor.Update(doctor);
            _context.SaveChanges();
        }
        public void DeleteDoctor(int id)
        {
            var doc = GetDoctor(id);
            if (doc != null)
            {
                _context.Doctor.Remove(doc);
                _context.SaveChanges();
            }
        }
        public bool UsernameExists(string username) => _context.User.Any(u => u.Username == username) || _context.Doctor.Any(d => d.Username == username);
    }
}
