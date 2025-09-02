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
    }
}
