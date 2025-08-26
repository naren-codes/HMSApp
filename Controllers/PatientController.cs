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

        // File: PatientController.cs

        public async Task<IActionResult> Dashboard()
        {
            // Get the username from the session.
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                // Redirect if the user is not logged in.
                return RedirectToAction("PatientLogin", "Account");
            }

            // Find the patient record using the username.
            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.Username == username);

            if (patient == null)
            {
                // Handle case where patient record is not found for the user.
                return RedirectToAction("PatientLogin", "Account");
            }

            // Pass the patient's name to the view.
            ViewData["PatientName"] = char.ToUpper(patient.Name[0]) + patient.Name.Substring(1);

            // Now, get the appointments using the correct PatientId.
            var appointments = await _patientService.GetPatientAppointmentsAsync(patient.PatientId);

            // Pass the appointments to the view.
            return View(appointments);
        }

    }
}
