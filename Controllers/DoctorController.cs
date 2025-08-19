using HMSApp.Data;
using HMSApp.Models;
using HMSApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class DoctorController : Controller
{
    private readonly DoctorService _doctorService;

    public DoctorController(DoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    public IActionResult Index()
    {
        var doctors = _doctorService.GetAllDoctors();
        return View(doctors);
    }
    public IActionResult DoctorDashboard()
    {
        return View();
    }
}