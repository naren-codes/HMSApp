namespace HMSApp.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }
        public string? Name { get; set; }
        public string? Specialization { get; set; }
        public string? ContactNumber { get; set; }
        public string? AvailabilitySchedule { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; } = "Doctor";
        public bool IsAvailable { get; set; } = true;
    }
}
