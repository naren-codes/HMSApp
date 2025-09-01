using HMSApp.Data;
using HMSApp.Models;
using HMSApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HMSApp.Controllers
{
    public class PatientController : Controller
    {
        private readonly PatientService _patientService;
        private readonly ApplicationDbContext _context;

        public PatientController(PatientService patientService, ApplicationDbContext context)
        {
            _patientService = patientService;
            _context = context;
        }



        public IActionResult Index()
        {
            var patients = _patientService.GetAllPatients();
            return View(patients);
        }

        public IActionResult Details(int id)
        {
            var patient = _patientService.GetPatientById(id);
            return View(patient);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Patient patient)
        {
            if (ModelState.IsValid)
            {
                _patientService.AddPatient(patient);
                return RedirectToAction("Index");
            }
            return View(patient);
        }

        public IActionResult Edit(int id)
        {
            var patient = _patientService.GetPatientById(id);
            return View(patient);
        }

        [HttpPost]
        public IActionResult Edit(Patient patient)
        {
            if (ModelState.IsValid)
            {
                _patientService.UpdatePatient(patient);
                return RedirectToAction("Index");
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors)
            {
                System.Diagnostics.Debug.WriteLine(error.ErrorMessage);
            }

            return View(patient);
        }

        public IActionResult Delete(int id)
        {
            var patient = _patientService.GetPatientById(id);
            return View(patient);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _patientService.DeletePatient(id);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Dashboard()
        {
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("PatientLogin", "Account");
            }
            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);

            if (patient == null)
            {
                return RedirectToAction("PatientLogin", "Account");
            }
            ViewData["PatientName"] = char.ToUpper(patient.Name[0]) + patient.Name.Substring(1);
            var appointments = await _patientService.GetPatientAppointmentsAsync(patient.PatientId);
            return View(appointments);
        }

        public IActionResult BookAppointment()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("PatientLogin", "Account");

            var patient = _context.Patient.FirstOrDefault(p => p.Username == username);
            if (patient == null)
                return RedirectToAction("PatientLogin", "Account");

            ViewBag.PatientName = patient.Name;

            var doctors = _context.Doctor
                .Select(d => new SelectListItem { Value = d.DoctorId.ToString(), Text = d.Specialization != null && d.Specialization != "" ? $"{d.Name} ({d.Specialization})" : d.Name })
                .ToList();
            ViewBag.Doctors = doctors;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookAppointment(Appointment model, string PatientDescription, string PatientName)
        {
            var username = HttpContext.Session.GetString("Username");
            var patient = _context.Patient.FirstOrDefault(p => p.Username == username);
            if (patient == null)
                return RedirectToAction("PatientLogin", "Account");


            model.PatientId = patient.PatientId;
            model.PatientName = string.IsNullOrWhiteSpace(PatientName) ? patient.Name : PatientName.Trim();
            model.PatientDescription = PatientDescription;

            if (!ModelState.IsValid)
            {
                ViewBag.PatientName = model.PatientName;
                var doctorsInvalid = _context.Doctor
                    .Select(d => new SelectListItem { Value = d.DoctorId.ToString(), Text = d.Specialization != null && d.Specialization != "" ? $"{d.Name} ({d.Specialization})" : d.Name })
                    .ToList();
                ViewBag.Doctors = doctorsInvalid;
                return View(model);
            }

            var doctor = _context.Doctor.FirstOrDefault(d => d.DoctorId == model.DoctorId);
            model.DoctorName = doctor?.Name;

            if (string.IsNullOrEmpty(model.Status))
            {
                model.Status = "Pending";
            }

            _context.Appointment.Add(model);
            _context.SaveChanges();

            return RedirectToAction(nameof(BookingConfirmation), new { id = model.AppointmentId });
        }

        public IActionResult BookingConfirmation(int id)
        {
            var appt = _context.Appointment.FirstOrDefault(a => a.AppointmentId == id);
            return View(appt);
        }


        public async Task<IActionResult> AppointmentHistory()
        {
            // Get the current user from the session, just like in your other methods
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("PatientLogin", "Account");
            }

            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
            if (patient == null)
            {
                // Patient not found, redirect to login
                return RedirectToAction("PatientLogin", "Account");
            }
 var appointments = await _context.Appointment
                                             .Where(a => a.PatientId == patient.PatientId)
                                             .OrderByDescending(a => a.AppointmentDate)
                                             .ToListAsync();

            return View(appointments);
        }


    }
}