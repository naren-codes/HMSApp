using HMSApp.Models;
using HMSApp.Services;
using Microsoft.AspNetCore.Mvc;
using HMSApp.Data;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
    private readonly AccountService _accountService;
    private readonly PatientService _patientService;
    private readonly ApplicationDbContext _context;

    public AccountController(AccountService accountService, PatientService patientService, ApplicationDbContext context)
    {
        _accountService = accountService;
        _patientService = patientService;
        _context = context;
    }

    [HttpGet]
    public IActionResult PatientLogin() => View();

    [HttpGet]
    public IActionResult AdminLogin() => View();

    [HttpGet]
    public IActionResult DoctorLogin() => View();

    private IActionResult HandleLogin(string username, string password, string expectedRole, string redirectController)
    {
        var user = _accountService.Authenticate(username, password);
        if (user != null && user.role?.ToLower() == expectedRole.ToLower())
        {
            return RedirectToAction("Index", redirectController);
        }
        ViewBag.Error = user == null ? "Invalid username or password" : $"{expectedRole} login is not allowed here.";
        return View($"{expectedRole}Login");
    }

    [HttpPost]
    public IActionResult LoginPatient(string username, string password)
    {
        // Step 1: Authenticate the user against the `User` table using the AccountService.
        var user = _accountService.Authenticate(username, password);

        if (user != null && user.role?.ToLower() == "patient")
        {
            // Step 2: If authentication is successful, use the username to find the corresponding patient record in the `Patient` table.
            // FIX: The original code was incorrectly searching the `Doctor` or `User` table for a patient.
            var patient = _context.Patient.FirstOrDefault(p => p.Username == username);

            if (patient != null)
            {
                // Step 3: If a patient record is found, store the username in the session.
                HttpContext.Session.SetString("Username", user.Username);
            }

            // Step 4: Redirect to the patient's dashboard.
            return RedirectToAction("Dashboard", "Patient");
        }

        // If authentication fails or the user is not a patient, show an error.
        ViewBag.Error = "Invalid login credentials.";
        return View("PatientLogin");



    }


    [HttpPost]
    public IActionResult LoginAdmin(string username, string password) => HandleLogin(username, password, "admin", "Admin");

    [HttpPost]
    public IActionResult LoginDoctor(string username, string password, string userType)
    {
        var user = _accountService.Authenticate(username, password);
        if (user != null && user.role?.ToLower() == "doctor")
        {
            // find doctor row
            var doctor = _context.Doctor.FirstOrDefault(d => d.Username == username);
            if (doctor != null)
            {
                HttpContext.Session.SetInt32("DoctorId", doctor.DoctorId);
            }
            return RedirectToAction("DoctorDashboard", "Doctor");
        }
        ViewBag.Error = "Invalid login credentials.";
        return View("DoctorLogin");
    }

    [HttpGet]
    public IActionResult PatientRegister()
    {
        return View();
    }

    [HttpPost]
    public IActionResult PatientRegister(Patient model)
    {
        if (ModelState.IsValid)
        {
            _accountService.RegisterPatient(model);
            TempData["SuccessMessage"] = "Registration successful! You can now sign in.";

            return RedirectToAction("PatientLogin");
        }
        return View(model);
    }


    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("DoctorLogin");
    }
}