using System.Diagnostics;
using HMSApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using HMSApp.Data;
using Microsoft.EntityFrameworkCore;

namespace HMSApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.DoctorCount = await _context.Doctor.CountAsync();
            ViewBag.PatientCount = await _context.Patient.CountAsync();
            ViewBag.AppointmentCount = await _context.Appointment.CountAsync();
            return View();
        }

        public IActionResult About() => View();
        public IActionResult Contact() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
