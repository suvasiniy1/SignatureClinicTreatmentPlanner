using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignatureClinicTreatmentPlanner.Data; // Ensure this namespace matches your project
using SignatureClinicTreatmentPlanner.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SignatureClinicTreatmentPlanner.Controllers
{
    public class ClinicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClinicController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Render the clinic list page
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

        // Fetch clinics from database and return JSON for the grid
        [HttpGet]
        public async Task<IActionResult> GetClinics()
        {
            var clinics = await _context.Clinics
                .Select(c => new { clinicID = c.ClinicID, clinicName = c.ClinicName }) // ✅ Ensure matching names
                .ToListAsync();

            return Json(new { data = clinics }); // ✅ Wrap inside "data"
        }

        // Create new clinic
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Clinic model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Validation failed: " + string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });
            }

            _context.Clinics.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Clinic added successfully" });
        }

        // Edit existing clinic
        [HttpPost]
        public async Task<IActionResult> UpdateClinicDetails([FromBody] Clinic model) // ✅ Add [FromBody]
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid data received" });
            }

            if (ModelState.IsValid)
            {
                _context.Clinics.Update(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Clinic updated successfully" });
            }
            return Json(new { success = false, message = "Validation failed" });
        }

        // Remove clinic
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var clinic = await _context.Clinics.FindAsync(id);
            if (clinic == null)
            {
                return Json(new { success = false, message = "Clinic not found" });
            }

            _context.Clinics.Remove(clinic);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Clinic removed successfully" });
        }
        [HttpGet]
        public async Task<IActionResult> GetClinic(int id)
        {
            var clinic = await _context.Clinics
                .Where(c => c.ClinicID == id)
                .Select(c => new { clinicID = c.ClinicID, clinicName = c.ClinicName })
                .FirstOrDefaultAsync();

            if (clinic == null)
            {
                return Json(new { success = false, message = "Clinic not found." });
            }

            return Json(clinic);
        }

    }
}
