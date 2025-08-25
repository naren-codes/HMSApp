using HMSApp.Data;
using HMSApp.Models;
using HMSApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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
            // Get the UserId from the session.
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                // Redirect if the user is not logged in.
                return RedirectToAction("PatientLogin", "Account");
            }
            var patient = _patientService.GetPatientById(userId.Value);
            ViewData["PatientName"] = patient?.Name ?? "Guest";
            // The Patient and User models are separate. Assuming Patient.PatientId is the same as User.Id.
            // If they are different, you'd need a way to find the Patient's ID from the User's ID.
            var appointments = await _patientService.GetPatientAppointmentsAsync(userId.Value);

            return View(appointments);
        }

    }
}
