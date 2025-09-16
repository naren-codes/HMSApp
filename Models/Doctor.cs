using System.ComponentModel.DataAnnotations;

namespace HMSApp.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }
        
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string? Name { get; set; }
        
        [Required(ErrorMessage = "Specialization is required.")]
        [StringLength(100, ErrorMessage = "Specialization cannot exceed 100 characters.")]
        public string? Specialization { get; set; }
        
        [Phone(ErrorMessage = "Invalid contact number format.")]
        public string? ContactNumber { get; set; }
        
        public string? AvailabilitySchedule { get; set; }
        
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string? Username { get; set; }
        
        [Required(ErrorMessage = "Password is required.")]
        [StrongPassword]
        public string? Password { get; set; }
        
        public string? Role { get; set; } = "Doctor";
        public bool IsAvailable { get; set; } = true;
    }
}
