using HMSApp.Models;
using HMSApp.Services;
using Microsoft.AspNetCore.Mvc;

public class AccountController : Controller
{
    private readonly AccountService _accountService;
    private readonly PatientService _patientService;

    public AccountController(AccountService accountService, PatientService patientService)
    {
        _accountService = accountService;
        _patientService = patientService;
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
        var user = _accountService.Authenticate(username, password);

        if (user != null && user.role?.ToLower() == "patient")
        {
            HttpContext.Session.SetInt32("UserId", user.userId); 
            return RedirectToAction("Dashboard", "Patient");
        }

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
            HttpContext.Session.SetString("doctorUsername", username);
            return RedirectToAction("DoctorDashboard", "Doctor");
        }
        ViewBag.Error = "Invalid login credentials.";
        return View("DoctorLogin");
    }

    [HttpPost]
    public IActionResult Register(Patient model)
    {
        if (ModelState.IsValid)
        {
            _accountService.RegisterPatient(model);
            return RedirectToAction("Login");
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