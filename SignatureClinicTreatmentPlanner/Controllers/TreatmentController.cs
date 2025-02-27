using Microsoft.AspNetCore.Mvc;
using SignatureClinicTreatmentPlanner.Data;
using SignatureClinicTreatmentPlanner.Models;
using Microsoft.EntityFrameworkCore;


namespace SignatureClinicTreatmentPlanner.Controllers
{
    public class TreatmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TreatmentController(ApplicationDbContext context)
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
        public JsonResult GetSurgeons()
        {
            var surgeons = _context.Surgeons
                .Select(s => new
                {
                    surgeonID = s.SurgeonID,  // ✅ Use lowercase for JavaScript compatibility
                    surgeonName = s.SurgeonName
                }).ToList();

            return Json(new { data = surgeons });  // ✅ Wrap response inside { data: [...] }
        }

        // Get List of Treatments with Assigned Surgeons
        [HttpGet]
        public async Task<IActionResult> GetTreatments()
        {
            var treatments = await _context.Treatments
                .Select(t => new {
                    treatmentID = t.TreatmentID,
                    treatmentName = t.TreatmentName, // ✅ Ensure correct property name
                    assignedSurgeons = _context.Surgeon_Treatment
                        .Where(st => st.TreatmentID == t.TreatmentID)
                        .Select(st => st.Surgeon.SurgeonName)
                        .ToList()
                })
                .ToListAsync();

            return Json(new { data = treatments }); // ✅ Wrap inside { "data": [...] }
        }


        // Save Treatment and Assign Surgeon
        [HttpPost]
        public JsonResult CreateTreatment(string treatmentName, int surgeonId)
        {
            if (string.IsNullOrEmpty(treatmentName) || surgeonId <= 0)
            {
                return Json(new { success = false, message = "Invalid data provided." });
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Check if Treatment Exists
                var treatment = _context.Treatments.FirstOrDefault(t => t.TreatmentName == treatmentName);
                if (treatment == null)
                {
                    treatment = new Treatment { TreatmentName = treatmentName };
                    _context.Treatments.Add(treatment);
                    _context.SaveChanges();
                }

                // Assign Surgeon to Treatment (Check if mapping exists)
                var existingAssignment = _context.Surgeon_Treatment
                    .FirstOrDefault(st => st.TreatmentID == treatment.TreatmentID && st.SurgeonID == surgeonId);

                if (existingAssignment == null)
                {
                    var assignment = new Surgeon_Treatment
                    {
                        TreatmentID = treatment.TreatmentID,
                        SurgeonID = surgeonId
                    };
                    _context.Surgeon_Treatment.Add(assignment);
                    _context.SaveChanges();
                }

                transaction.Commit();
                return Json(new { success = true, message = "Treatment and Surgeon assigned successfully." });
            }
            catch
            {
                transaction.Rollback();
                return Json(new { success = false, message = "Error while saving treatment." });
            }
        }

        // Edit Treatment - Get Details
        [HttpGet]
        public JsonResult GetTreatment(int id)
        {
            var treatment = _context.Treatments
                .Where(t => t.TreatmentID == id)
                .Select(t => new
                {
                    treatmentID = t.TreatmentID,
                    treatmentName = t.TreatmentName, // ✅ Ensure property name is correct
                    assignedSurgeons = _context.Surgeon_Treatment
                        .Where(st => st.TreatmentID == t.TreatmentID)
                        .Select(st => new
                        {
                            surgeonID = st.Surgeon.SurgeonID,
                            surgeonName = st.Surgeon.SurgeonName
                        })
                        .ToList()
                })
                .FirstOrDefault();

            if (treatment == null)
                return Json(new { success = false, message = "Treatment not found." });

            return Json(treatment);
        }


        // Delete Treatment and Assigned Surgeon
        [HttpPost]
        public JsonResult RemoveTreatment(int id)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Remove Surgeon-Treatment Assignments
                var assignments = _context.Surgeon_Treatment.Where(st => st.TreatmentID == id).ToList();
                _context.Surgeon_Treatment.RemoveRange(assignments);

                // Remove Treatment Record
                var treatment = _context.Treatments.Find(id);
                if (treatment != null)
                {
                    _context.Treatments.Remove(treatment);
                }

                _context.SaveChanges();
                transaction.Commit();
                return Json(new { success = true, message = "Treatment deleted successfully." });
            }
            catch
            {
                transaction.Rollback();
                return Json(new { success = false, message = "Error while deleting treatment." });
            }
        }
        [HttpPost]
        public JsonResult UpdateTreatment(int treatmentId, string treatmentName)
        {
            if (treatmentId <= 0 || string.IsNullOrWhiteSpace(treatmentName))
            {
                return Json(new { success = false, message = "Invalid data provided." });
            }

            var treatment = _context.Treatments.FirstOrDefault(t => t.TreatmentID == treatmentId);
            if (treatment == null)
            {
                return Json(new { success = false, message = "Treatment not found." });
            }

            treatment.TreatmentName = treatmentName; // ✅ Only update treatment name
            _context.SaveChanges();

            return Json(new { success = true, message = "Treatment name updated successfully." });
        }
        [HttpGet]
        public JsonResult GetSurgeonsByTreatment(int treatmentId)
        {
            var surgeons = _context.Surgeon_Treatment
                .Where(st => st.TreatmentID == treatmentId)
                .Include(st => st.Surgeon)
                .Select(st => new
                {
                    surgeonID = st.Surgeon.SurgeonID,  // ✅ Match JavaScript key names
                    surgeonName = st.Surgeon.SurgeonName
                })
                .ToList();

            return Json(new { data = surgeons });  // ✅ Wrap response in { data: [...] }
        }

    }
}
