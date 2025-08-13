namespace HMSApp.Models
{
    public class Bill
    {
        public int BillId { get; set; }
        public int PatientId { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime BillDate { get; set; }
    }
}
