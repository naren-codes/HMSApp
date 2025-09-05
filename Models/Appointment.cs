using System.ComponentModel.DataAnnotations;

namespace HMSApp.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? TimeSlot { get; set; }
        public string? Status { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        [StringLength(255)]
        public string? Symptoms { get; set; }
        public string? Prescription { get; set; }
    }
}