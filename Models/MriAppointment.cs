
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace HMSApp.Models
{
    /// <summary>
    /// Represents an MRI appointment with patient and lab details.
    /// </summary>
    public class MriAppointment
    {
        /// <summary>
        /// Gets or sets the unique identifier for the appointment.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the patient.
        /// </summary>
        public string PatientName { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the appointment.
        /// </summary>
        public DateTime AppointmentDate { get; set; }

        /// <summary>
        /// Gets or sets the name of the lab where the MRI will be performed.
        /// </summary>
        public string LabName { get; set; }
    }
}

