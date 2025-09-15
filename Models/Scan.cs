using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HMSApp.Models
{
    public class Scan
    {
        // The [Key] attribute explicitly defines this as the primary key.
        [Key]
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }

        public DateTime AppointmentDate { get; set; }


        public string LabName { get; set; }
        public string ScanType { get; set; }
        public string? FileName { get; set; }

        public int? DoctorId { get; set; }

        // Store doctor's display name instead of a complex object
        public string? DoctorName { get; set; }
    }
}
