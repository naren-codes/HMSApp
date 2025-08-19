using HMSApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HMSApp.Data;
using System;

namespace HMSApp.Services
{
    public class PatientService
    {
        private readonly ApplicationDbContext _context;

        public object Patient { get; internal set; }

        public PatientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Patient> GetAllPatients() =>
            _context.Patient.ToList();

        public Patient? GetPatientById(int id) =>
            _context.Patient.FirstOrDefault(p => p.PatientId == id);

        public void AddPatient(Patient patient)
        {
            _context.Patient.Add(patient);
            _context.SaveChanges();
        }

        public void UpdatePatient(Patient patient)
        {
            _context.Patient.Update(patient);
            _context.SaveChanges();
        }

        public void DeletePatient(int id)
        {
            var patient = _context.Patient.FirstOrDefault(p => p.PatientId == id);
            if (patient != null)
            {
                _context.Patient.Remove(patient);
                _context.SaveChanges();
            }
        }




    }
}
