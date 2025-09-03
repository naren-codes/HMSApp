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

        // Admin-facing list of all patients
        public IActionResult Index()
        {
            var patients = _patientService.GetAllPatients();
            return View(patients);
        }

        // Admin-facing patient details
        public IActionResult Details(int id)
        {
            var patient = _patientService.GetPatientById(id);
            return View(patient);
        }

        // Admin-facing create patient form (GET)
        public IActionResult Create() => View();

        // Admin-facing create patient form (POST)
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

        // Admin-facing edit patient form (GET)
        public IActionResult Edit(int id)
        {
            var patient = _patientService.GetPatientById(id);
            return View(patient);
        }

        // Admin-facing edit patient form (POST)
        [HttpPost]
        public IActionResult Edit(Patient patient)
        {
            if (ModelState.IsValid)
            {
                _patientService.UpdatePatient(patient);
                return RedirectToAction("Index");
            }
            return View(patient);
        }

        // Admin-facing delete confirmation page (GET)
        public IActionResult Delete(int id)
        {
            var patient = _patientService.GetPatientById(id);
            return View(patient);
        }

        // Admin-facing delete confirmation (POST)
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            _patientService.DeletePatient(id);
            return RedirectToAction("Index");
        }

        // Patient-facing dashboard
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
            ViewData["PatientContact"] = patient.ContactNumber ?? "N/A";
            ViewData["PatientAddress"] = patient.Address ?? "N/A";
            ViewData["PatientGender"] = patient.Gender ?? "N/A";

            ViewData["PatientName"] = char.ToUpper(patient.Name[0]) + patient.Name.Substring(1);
            var appointments = await _patientService.GetPatientAppointmentsAsync(patient.PatientId);
            return View(appointments);
        }

        // Patient-facing "Book Appointment" page (GET)
        public IActionResult BookAppointment()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("PatientLogin", "Account");

            var patient = _context.Patient.FirstOrDefault(p => p.Username == username);
            if (patient == null)
                return RedirectToAction("PatientLogin", "Account");

            ViewBag.PatientName = patient.Name;
            ViewBag.Doctors = _context.Doctor
                .Select(d => new SelectListItem { Value = d.DoctorId.ToString(), Text = d.Specialization != null && d.Specialization != "" ? $"{d.Name} ({d.Specialization})" : d.Name })
                .ToList();
            return View();
        }

        // Patient-facing "Book Appointment" form submission (POST)
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
            model.Status = "Pending";

            if (!ModelState.IsValid)
            {
                ViewBag.PatientName = model.PatientName;
                ViewBag.Doctors = _context.Doctor
                    .Select(d => new SelectListItem { Value = d.DoctorId.ToString(), Text = d.Specialization != null && d.Specialization != "" ? $"{d.Name} ({d.Specialization})" : d.Name })
                    .ToList();
                return View(model);
            }

            var doctor = _context.Doctor.FirstOrDefault(d => d.DoctorId == model.DoctorId);
            model.DoctorName = doctor?.Name;

            _context.Appointment.Add(model);
            _context.SaveChanges();

            return RedirectToAction(nameof(BookingConfirmation), new { id = model.AppointmentId });
        }

        // Patient-facing confirmation page after booking
        public IActionResult BookingConfirmation(int id)
        {
            var appt = _context.Appointment.FirstOrDefault(a => a.AppointmentId == id);
            return View(appt);
        }

        // Patient-facing appointment history page
        public async Task<IActionResult> AppointmentHistory()
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
            var appointments = await _context.Appointment
                                             .Where(a => a.PatientId == patient.PatientId)
                                             .OrderByDescending(a => a.AppointmentDate)
                                             .ToListAsync();
            return View(appointments);
        }

        // Patient-facing profile page
        public async Task<IActionResult> Profile()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Patient patient)
        {
            // The [Bind] attribute is a security best practice to prevent over-posting.
            // We only bind the fields that are actually on the form.
            if (ModelState.IsValid)
            {
                try
                {
                    // Using the service to update is good practice.
                    _patientService.UpdatePatient(patient);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // This handles a rare case where the data might have been deleted by another user
                    // between the time the patient loaded the page and saved it.
                    if (_patientService.GetPatientById(patient.PatientId) == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                // After a successful save, redirect back to the profile page to show the updated info.
                return RedirectToAction(nameof(Dashboard));
            }
           
            return View("Profile", patient);
        }

    }
}

