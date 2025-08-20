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

        if (user != null)
        {
            if (user.role?.ToLower() == expectedRole.ToLower())
            {
                // HttpContext.Session.SetString("Username", user.Username); // Optional
                return RedirectToAction("Index", redirectController);
            }
            else
            {
                ViewBag.Error = $"{expectedRole} login is not allowed here.";
                return View($"{expectedRole}Login");
            }
        }

        ViewBag.Error = "Invalid username or password";
        return View($"{expectedRole}Login");
    }

    [HttpPost]
    public IActionResult LoginPatient(string username, string password)
    {
        return HandleLogin(username, password, "patient", "Home");
    }

    [HttpPost]
    public IActionResult LoginAdmin(string username, string password)
    {
        return HandleLogin(username, password, "admin", "Admin");
    }

    [HttpPost]
    public IActionResult LoginDoctor(string username, string password)
    {
        var user = _accountService.Authenticate(username, password);
        if (user != null)
        {
            if (user.role?.ToLower() == "doctor")
            {
                return RedirectToAction("DoctorDashboard", "Doctor", new { u = username });
            }
            ViewBag.Error = "doctor login is not allowed here.";
            return View("DoctorLogin");
        }
        ViewBag.Error = "Invalid username or password";
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
}