using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignatureClinicTreatmentPlanner.Data;
using SignatureClinicTreatmentPlanner.Models;

namespace SignatureClinicTreatmentPlanner.Controllers
{
    public class SurgeonController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SurgeonController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.UserInitials = HttpContext.Session.GetString("UserInitials");
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetSurgeons()
        {
            try
            {
                var surgeons = await _context.Surgeons
                    .Select(s => new
                    {
                        surgeonID = s.SurgeonID,  // ✅ Ensure lowercase for JSON keys
                        surgeonName = s.SurgeonName,  // ✅ Match DataTable column names
                        assignedClinics = _context.Surgeon_Clinic
                            .Where(sc => sc.SurgeonID == s.SurgeonID)
                            .Select(sc => sc.Clinic.ClinicName)
                            .ToList()
                    })
                    .ToListAsync();

                return Json(new { data = surgeons }); // ✅ Wrap inside { "data": [...] }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching surgeons", error = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetClinicsBySurgeon(int surgeonId)
        {
            var clinics = _context.Surgeon_Clinic
                .Where(sc => sc.SurgeonID == surgeonId)
                .Include(sc => sc.Clinic)
                .Select(sc => new
                {
                    ClinicID = sc.Clinic.ClinicID,
                    ClinicName = sc.Clinic.ClinicName
                }).ToList();

            return Json(clinics);
        }
        [HttpGet]
        [Route("Surgeon/GetClinics")]
        public JsonResult GetClinics()
        {
            try
            {
                var clinics = _context.Clinics
                    .Select(c => new
                    {
                        clinicID = c.ClinicID,  // ✅ Lowercase keys
                        clinicName = c.ClinicName
                    })
                    .ToList();

                return Json(new { data = clinics });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching clinics", error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CreateSurgeon(string surgeonName, int clinicId)
        {
            if (string.IsNullOrWhiteSpace(surgeonName) || clinicId <= 0)
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            var existingSurgeon = _context.Surgeons.FirstOrDefault(s => s.SurgeonName == surgeonName);
            if (existingSurgeon == null)
            {
                var newSurgeon = new Surgeon { SurgeonName = surgeonName };
                _context.Surgeons.Add(newSurgeon);
                _context.SaveChanges();
                existingSurgeon = newSurgeon; // Get the newly created surgeon
            }

            var existingAssignment = _context.Surgeon_Clinic
                .FirstOrDefault(sc => sc.SurgeonID == existingSurgeon.SurgeonID && sc.ClinicID == clinicId);

            if (existingAssignment == null)
            {
                var assignment = new Surgeon_Clinic
                {
                    SurgeonID = existingSurgeon.SurgeonID,
                    ClinicID = clinicId
                };
                _context.Surgeon_Clinic.Add(assignment);
                _context.SaveChanges();
            }

            return Json(new { success = true, message = "Surgeon created and clinic assigned successfully." });
        }
        [HttpPost]
        public JsonResult UpdateSurgeon(int surgeonId, string surgeonName)
        {
            if (surgeonId <= 0 || string.IsNullOrWhiteSpace(surgeonName))
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            var surgeon = _context.Surgeons.FirstOrDefault(s => s.SurgeonID == surgeonId);
            if (surgeon == null)
            {
                return Json(new { success = false, message = "Surgeon not found." });
            }

            surgeon.SurgeonName = surgeonName; // ✅ Only update the name
            _context.SaveChanges();

            return Json(new { success = true, message = "Surgeon name updated successfully." });
        }

        [HttpPost]
        public JsonResult DeleteSurgeon(int surgeonId)
        {
            try
            {
                var surgeon = _context.Surgeons.FirstOrDefault(s => s.SurgeonID == surgeonId);
                if (surgeon == null)
                {
                    return Json(new { success = false, message = "Surgeon not found." });
                }

                // ✅ Remove associated clinic assignments
                var surgeonClinics = _context.Surgeon_Clinic.Where(sc => sc.SurgeonID == surgeonId).ToList();
                if (surgeonClinics.Any())
                {
                    _context.Surgeon_Clinic.RemoveRange(surgeonClinics);
                }

                // ✅ Delete the surgeon
                _context.Surgeons.Remove(surgeon);
                _context.SaveChanges();

                return Json(new { success = true, message = "Surgeon deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting surgeon.", error = ex.Message });
            }
        }

    }
}
