using HMSApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace HMSApp.ViewModels
{
    public class AppointmentHistoryViewModel
    {
        public IEnumerable<Appointment> Appointment { get; set; } = new List<Appointment>();
        public IEnumerable<Scan> Scan { get; set; } = new List<Scan>();

        public List<SelectListItem> AvailableDoctors { get; set; } = new List<SelectListItem>();
    }
}