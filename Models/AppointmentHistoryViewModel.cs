using HMSApp.Models;
using System.Collections.Generic;

namespace HMSApp.ViewModels
{
    public class AppointmentHistoryViewModel
    {
        public IEnumerable<Appointment> Appointment { get; set; }
        public IEnumerable<Scan> Scan { get; set; }
    }
}