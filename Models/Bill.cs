using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HMSApp.Models
{
    [Table("Bill")]
    public class Bill
    {
        [Key]
        [Column("billId")]
        public int BillId { get; set; }

        [Column("patientId")]
        public int PatientId { get; set; }

        [Column("appointmentId")]
        public int? AppointmentId { get; set; }

        // Additional fields for robust matching across environments
        [Column("appointmentDate")]
        public DateTime? AppointmentDate { get; set; }

        [Column("doctorName")]
        public string? DoctorName { get; set; }

        [Column("timeSlot")]
        public string? TimeSlot { get; set; }

        [Column("totalAmount")]
        public decimal TotalAmount { get; set; }

        [Column("paymentStatus")]
        [Required]
        public string PaymentStatus { get; set; } = string.Empty; 

        [Column("billDate")]
        public DateTime BillDate { get; set; }

        [Column("patientName")]
        public string? PatientName { get; set; }

        [Column("prescription")]
        public string? Prescription { get; set; }

        [NotMapped]
        public string PaymentMethod => PaymentStatus.StartsWith("Paid-") ? PaymentStatus.Split('-').Last() : (PaymentStatus == "Paid" ? "Unknown" : "");

        [NotMapped]
        public string DoctorName_Legacy { get; set; } = string.Empty; 
    }
}
