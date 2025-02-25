using Microsoft.AspNetCore.Mvc;

namespace SignatureClinicTreatmentPlanner.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Dashboard()
        {
            // Check if the user is logged in
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToAction("Index", "Login");
            }

            // Bind the session value to ViewBag
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.FullName = HttpContext.Session.GetString("FullName"); // If you have a full name stored
            ViewBag.UserInitials = HttpContext.Session.GetString("UserInitials");
            return View();
        }
    }

}
