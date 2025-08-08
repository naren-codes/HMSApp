using System;
using System.ComponentModel.DataAnnotations;

namespace HMSApp.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string? Gender { get; set; }

        [Phone]
        public string? ContactNumber { get; set; }

        public string? Address { get; set; }

        public string? MedicalHistory { get; set; }
    }
}
