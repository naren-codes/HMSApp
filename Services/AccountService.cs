using HMSApp.Data;
using HMSApp.Models;

namespace HMSApp.Services
{
    public class AccountService
    {
        private readonly ApplicationDbContext _context;

        public AccountService(ApplicationDbContext context)
        {
            _context = context;
        }

        public User? Authenticate(string username, string password)
        {
            return _context.User.FirstOrDefault(u => u.Username == username && u.Password == password);
        }

        public void RegisterPatient(Patient model)
        {
            var patient = new Patient
            {
                Name = model.Name,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                ContactNumber = model.ContactNumber,
                Address = model.Address,
                Username=model.Username,
                Password = model.Password,
                Role=model.Role

            };
            _context.Patient.Add(patient);
            //_context.SaveChanges();

            var user = new User
            {
                Username = model.Username,
                Password = model.Password,
                role = model.Role
            };
            _context.User.Add(user);
            _context.SaveChanges();
        }

        public void RegisterDoctor(Doctor model)
        {
            var doctor = new Doctor
            {
                Name = model.Name,
                Specialization = model.Specialization,
                ContactNumber = model.ContactNumber,
                AvailabilitySchedule = model.AvailabilitySchedule,
                Username = model.Username,
                Password = model.Password,
                Role = model.Role
            };
            _context.Doctor.Add(doctor);
            _context.SaveChanges();

            var user = new User
            {
                Username = model.Username,
                Password = model.Password,
                role = model.Role
            };
            _context.User.Add(user);
            _context.SaveChanges();
        }
    }
}