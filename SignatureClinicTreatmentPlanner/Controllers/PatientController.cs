using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SignatureClinicTreatmentPlanner.Data;
using SignatureClinicTreatmentPlanner.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace SignatureClinicTreatmentPlanner.Controllers
{
    [Route("Patient")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public PatientController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Patient/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            // Get the logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.UserName = user.UserName; // Pass the username to the view
            }

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync(); // Make it async

                // Fetch Treatments manually
                var treatments = new List<Treatment>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT TreatmentID, TreatmentName FROM Treatments";
                    using (var reader = await command.ExecuteReaderAsync()) // Make it async
                    {
                        while (await reader.ReadAsync()) // Make it async
                        {
                            treatments.Add(new Treatment
                            {
                                TreatmentID = reader.GetInt32(0),
                                TreatmentName = reader.GetString(1)
                            });
                        }
                    }
                }
                ViewBag.Treatments = treatments;

                // Fetch Surgeons manually
                var surgeons = new List<Surgeon>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT SurgeonID, SurgeonName FROM Surgeons";
                    using (var reader = await command.ExecuteReaderAsync()) // Make it async
                    {
                        while (await reader.ReadAsync()) // Make it async
                        {
                            surgeons.Add(new Surgeon
                            {
                                SurgeonID = reader.GetInt32(0),
                                SurgeonName = reader.GetString(1)
                            });
                        }
                    }
                }
                ViewBag.Surgeons = surgeons;

                // Fetch Clinics manually
                var clinics = new List<Clinic>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT ClinicID, ClinicName FROM Clinics";
                    using (var reader = await command.ExecuteReaderAsync()) // Make it async
                    {
                        while (await reader.ReadAsync()) // Make it async
                        {
                            clinics.Add(new Clinic
                            {
                                ClinicID = reader.GetInt32(0),
                                ClinicName = reader.GetString(1)
                            });
                        }
                    }
                }
                ViewBag.Clinics = clinics;
            }

            return View();
        }
        [HttpPost("Create")] // Ensure it explicitly allows POST requests
        public IActionResult Create(Patient model)
        {
            if (ModelState.IsValid)
            {
                _context.Patients.Add(model);
                _context.SaveChanges();
                return RedirectToAction("GeneratePdf", "Pdf", new { id = model.Id });
            }
            return View(model);
        }


        // POST: /Patient/Create
        //[HttpPost]
        //public IActionResult Create(Patient model,List<int> SurgeonIDs)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Add patient to the database
        //        _context.Patients.Add(model);
        //        _context.SaveChanges(); // Save to get the patient's Id

        //        // Save the selected surgeons in the PatientSurgeon table
        //        if (SurgeonIDs != null && SurgeonIDs.Count > 0)
        //        {
        //            foreach (var surgeonId in SurgeonIDs)
        //            {
        //                var patientSurgeon = new PatientSurgeons
        //                {
        //                    PatientId = model.Id,
        //                    SurgeonId = surgeonId
        //                };
        //                _context.PatientSurgeons.Add(patientSurgeon);
        //            }
        //            _context.SaveChanges(); // Save changes for the PatientSurgeon table
        //        }

        //        // Redirect to PdfController's GeneratePdf action with the new Patient ID
        //        return RedirectToAction("GeneratePdf", "Pdf", new { id = model.Id });
        //    }

        //    // If validation fails, show the form again
        //    return View(model);
        //}
        // Fetch Surgeons by Treatment
        [HttpGet("GetSurgeonsByTreatment")]
        public IActionResult GetSurgeonsByTreatment(int treatmentID)
        {
            var surgeons = _context.Surgeons
                .Where(s => _context.Surgeon_Treatment
                    .Any(st => st.SurgeonID == s.SurgeonID && st.TreatmentID == treatmentID))
                .Select(s => new { surgeonID = s.SurgeonID, surgeonName = s.SurgeonName }) // Ensure JSON property names match
                .ToList();

            return Json(surgeons);
        }
        // Fetch Clinics by Surgeons


        [HttpGet("GetClinicsBySurgeon")]
        public IActionResult GetClinicsBySurgeon(int surgeonID)  // Accept a single integer
        {
            if (surgeonID <= 0)
            {
                return Json(new { success = false, message = "Invalid surgeon ID." });
            }

            // Fetch clinics associated with this single surgeon
            var clinics = _context.Clinics
                .Where(c => _context.Surgeon_Clinic
                    .Any(sc => sc.ClinicID == c.ClinicID && sc.SurgeonID == surgeonID))  // Match a single ID
                .Select(c => new
                {
                    ClinicID = c.ClinicID,  // Ensure JSON matches frontend expectations
                    ClinicName = c.ClinicName
                })
                .Distinct()
                .ToList();

            return Json(clinics);
        }
    }
}
